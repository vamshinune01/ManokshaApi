using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using FirebaseAdmin;
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
            // Initialize Firebase using service account
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("firebase-service.json")
                });
            }

            // ✅ Explicitly create StorageClient using same credentials
            var credential = GoogleCredential.FromFile("firebase-service.json");
            _storageClient = StorageClient.Create(credential);

            // ✅ Use the exact bucket name you saw in Firebase
            _bucketName = "manoksha-collections.firebasestorage.app";
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var obj = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: fileName,
                contentType: contentType,
                source: fileStream
            );

            // ✅ Return public download URL
            return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{obj.Name}?alt=media";
        }
    }
}
