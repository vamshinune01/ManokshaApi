using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.IO;
using System.Threading.Tasks;

namespace ManokshaApi.Services
{
    public class FirebaseStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public FirebaseStorageService()
        {
            // Initialize Firebase app only once
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("firebase-service.json")
                });
            }

            // ✅ Create StorageClient using the same credentials
            var credential = GoogleCredential.FromFile("firebase-service.json");
            _storageClient = StorageClient.Create(credential);

            // ✅ Your actual bucket name
            _bucketName = "manoksha-collections.appspot.com";
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var obj = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: fileName,
                contentType: contentType,
                source: fileStream
            );

            // Return public URL
            return $"https://storage.googleapis.com/{_bucketName}/{obj.Name}";
        }
    }
}
