using System.Security.Cryptography;
using System.Text;

namespace ImageService
{
    public class FileHasher
    {
        public string ComputeSha256Hash(byte[] fileBytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(fileBytes);
                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

        public string ComputeSha256Hash(string url) => 
            ComputeSha256Hash(Encoding.UTF8.GetBytes(url));
    }
}