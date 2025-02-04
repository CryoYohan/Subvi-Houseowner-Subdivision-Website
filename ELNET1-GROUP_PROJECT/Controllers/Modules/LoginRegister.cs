using Azure.Identity;

namespace Subvi.Controllers.Modules
{
    // SQL Server in Lab 530 is broken, might use InMemoryDatabase for testing in Lab.
    public class LoginRegister
    {
        public LoginRegister() { }

        public void Login(string username, string password) 
        {
            // Some logic here for authorization, needs database connection
        }

        public void Register(string username, string email,string password) 
        { 
            // Some logic here for registration, needs database connection
        }


    }
}
