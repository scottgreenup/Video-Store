using System;
using System.Data;
using System.Linq;
using System.Transactions;
using Bank.Business.Components.Interfaces;
using Bank.Business.Entities;

namespace Bank.Business.Components {
    public class TransferProvider : ITransferProvider {
        public void Transfer(double amount, int fromAccountNo, int toAccountNo)
        {
            using (TransactionScope scope = new TransactionScope())
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer()) {

                // The lContainer is an ObjectContext. It's the primary way for interacting with
                // entities. It encapsulates a connection to the database, contains metadata for
                // the model, and contains an ObjectStateManager which manages objects in the cache


                try {
                    Account fromAccount = GetAccountFromNumber(fromAccountNo);
                    Account toAccount = GetAccountFromNumber(toAccountNo);

                    // Perform the transfer on the local instances of the entities.
                    fromAccount.Withdraw(amount);
                    toAccount.Deposit(amount);

                    // Attach the accounts to the object context. This is done because we know the
                    // entity already exists. It's just not being tracked by this context.
                    lContainer.Attach(fromAccount);
                    lContainer.Attach(toAccount);

                    

                    // Change the state of the entity (ObjectStateEntry) for the specific object.
                    // Some info from https://msdn.microsoft.com/en-gb/data/jj592676
                    // Added     - Tracked by lContainer, does not exist in DB
                    // Unchanged - Tracked by lContainer, exists, and has not changed from the 
                    //             values in the database
                    // Modified  - Tracked by lContainer, exists, but properties have been changed.
                    // Deleted   - Tracked, exists, marked for deletion on SaveChanges()
                    // Detached  - Not tracked.
                    var osm = lContainer.ObjectStateManager;
                    osm.ChangeObjectState(fromAccount, EntityState.Modified);
                    osm.ChangeObjectState(toAccount, EntityState.Modified);

                    // Modified entities are updated in the database and are now Unchanged.
                    lContainer.SaveChanges();
                    scope.Complete();

                } catch (Exception lException) {
                    Console.WriteLine(
                        "Error occured while transferring money: " + lException.Message);
                    throw;
                }
            }
        }

        private Account GetAccountFromNumber(int pToAcctNumber)
        {
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer()) {
                return lContainer.Accounts.Where((pAcct) => (pAcct.AccountNumber == pToAcctNumber)).FirstOrDefault();
            }
        }
    }
}
