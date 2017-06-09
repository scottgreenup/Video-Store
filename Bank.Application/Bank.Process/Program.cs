using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Bank.Business.Entities;
using System.ServiceModel;
using Bank.Services;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using Microsoft.Practices.Unity.ServiceLocatorAdapter;
using Microsoft.Practices.ServiceLocation;
using System.Configuration;

namespace Bank.Process {
    class Program {
        static void Main(string[] args)
        {
            ResolveDependencies();
            CreateDummyEntities();
            HostServices();

        }

        private static void HostServices()
        {
            using (ServiceHost lHost = new ServiceHost(typeof(TransferService))) {
                lHost.Open();
                Console.WriteLine("Bank Services started. Press Q to quit.");
                while (Console.ReadKey().Key != ConsoleKey.Q) ;
            }
        }

        private static void CreateDummyEntities()
        {
            using (TransactionScope lScope = new TransactionScope())
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer()) {
                if (lContainer.Accounts.Count() == 0) {
                    Customer lVideoStore = new Customer();
                    Account lVSAccount = new Account() {
                        AccountNumber = 123,
                        Balance = 0
                    };
                    lVideoStore.Accounts.Add(lVSAccount);

                    Customer lCustomer = new Customer();
                    Account lCustAccount = new Account() {
                        AccountNumber = 456,
                        Balance = 20
                    };
                    lCustomer.Accounts.Add(lCustAccount);

                    lContainer.Customers.AddObject(lVideoStore);
                    lContainer.Customers.AddObject(lCustomer);
                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }

        // Does some voodoo black magic.
        private static void ResolveDependencies()
        {
            // Creates an dependency injection engine
            UnityContainer lContainer = new UnityContainer();

            // Grabs the configuration from App.config
            UnityConfigurationSection lSection = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");

            // Set the container to our IOC.
            lSection.Containers["containerOne"].Configure(lContainer);
            
            // Setup ServiceLocator to use Unity which will use the UnityContainer
            UnityServiceLocator locator = new UnityServiceLocator(lContainer);
            ServiceLocator.SetLocatorProvider(() => locator);
        }
    }
}
