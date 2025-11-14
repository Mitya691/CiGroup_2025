using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Services
{
    public interface IPasswordHasher
    {
        string NewSalt(int size = 16);
        string Hash(string password, string base64Salt, int iterations = 100_000);
        bool Verify(string password, string base64Salt, string base64Hash, int iterations = 100_000);

    }
}
