using Common;
using DeliveryCo.MessageTypes;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Messaging;
using System.Transactions;
using VideoStore.Business.Components.Interfaces;
using VideoStore.Business.Entities;

namespace VideoStore.Business.Components {
    public class OrderProvider : IOrderProvider {
        public IEmailProvider EmailProvider {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public IUserProvider UserProvider {
            get { return ServiceLocator.Current.GetInstance<IUserProvider>(); }
        }

        public void SubmitOrder(Order pOrder)
        {
            Logging.Info($"Order:{pOrder.OrderNumber} : Submitting");

            using (TransactionScope lScope = new TransactionScope()) {
                LoadMediaStocks(pOrder);
                MarkAppropriateUnchangedAssociations(pOrder);
                using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer()) {
                    try {
                        pOrder.OrderNumber = Guid.NewGuid();
                        TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0);

                        pOrder.UpdateStockLevels();

                        PlaceDeliveryForOrder(pOrder);
                        lContainer.Orders.ApplyChanges(pOrder);

                        lContainer.SaveChanges();
                        lScope.Complete();

                    } catch (Exception lException) {
                        SendOrderErrorMessage(pOrder, lException);
                        throw;
                    }
                }
            }
            SendOrderPlacedConfirmation(pOrder);
        }

        private void MarkAppropriateUnchangedAssociations(Order pOrder)
        {
            pOrder.Customer.MarkAsUnchanged();
            pOrder.Customer.LoginCredential.MarkAsUnchanged();
            foreach (OrderItem lOrder in pOrder.OrderItems) {
                lOrder.Media.Stocks.MarkAsUnchanged();
                lOrder.Media.MarkAsUnchanged();
            }
        }

        private void LoadMediaStocks(Order pOrder)
        {
            using (VideoStoreEntityModelContainer lContainer = new VideoStoreEntityModelContainer()) {
                foreach (OrderItem lOrder in pOrder.OrderItems) {
                    lOrder.Media.Stocks = lContainer.Stocks.Where((pStock) => pStock.Media.Id == lOrder.Media.Id).FirstOrDefault();
                }
            }
        }



        private void SendOrderErrorMessage(Order pOrder, Exception pException)
        {
            Logging.Info($"Order:{pOrder.OrderNumber} : Had an error during processing: {pException.Message}");
            EmailProvider.SendMessage(new EmailMessage() {
                ToAddress = pOrder.Customer.Email,
                Message = "There was an error in processsing your order " + pOrder.OrderNumber + ": " + pException.Message + ". Please contact Video Store"
            });
        }

        private void SendOrderPlacedConfirmation(Order pOrder)
        {
            Logging.Info($"Order:{pOrder.OrderNumber} : Successfully submitted");
            EmailProvider.SendMessage(new EmailMessage() {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has been placed"
            });
        }

        private void PlaceDeliveryForOrder(Order pOrder)
        {
            Delivery lDelivery = new Delivery() { DeliveryStatus = DeliveryStatus.Submitted, SourceAddress = "Video Store Address", DestinationAddress = pOrder.Customer.Address, Order = pOrder };

            Guid lDeliveryIdentifier = ExternalServiceFactory.Instance.DeliveryService.SubmitDelivery(new DeliveryInfo() {
                OrderNumber = lDelivery.Order.OrderNumber.ToString(),
                SourceAddress = lDelivery.SourceAddress,
                DestinationAddress = lDelivery.DestinationAddress,
                DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService"
            });

            lDelivery.ExternalDeliveryIdentifier = lDeliveryIdentifier;
            pOrder.Delivery = lDelivery;

        }

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal)
        {
            try {
                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveVideoStoreAccountNumber());
            } catch (Exception e) {
                throw new Exception("Error Transferring funds for order.");
            }
        }


        private int RetrieveVideoStoreAccountNumber()
        {
            return 123;
        }


    }
}
