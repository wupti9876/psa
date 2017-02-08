using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace PhotoSharingApplication.Models
{
    public class PhotoSharingInitializer : CreateDatabaseIfNotExists<PhotoSharingContext>
    {
        //This method puts sample data into the database
        protected override void Seed(PhotoSharingContext context)
        {
            base.Seed(context);

            //Create some photos
            var photos = new List<Photo>
            {
                new Photo {
                    Title = "Sample Photo 1",
                    Description = "This is the first sample photo in the Adventure Works photo application",
                    UserName = "AllisonBrown",
                    //PhotoFile = getFileBytes("\\Images\\flower.jpg"),
                    AzurePath = storeInAzure("Images\\flower.jpg"),
                    ImageMimeType = "image/jpeg",
                    CreatedDate = DateTime.Today.AddDays(-5),
                    Location = "Paris, France",
                    Latitude = "48.85693",
                    Longitude = "2.3412"
                },
                new Photo {
                    Title = "Sample Photo 2",
                    Description = "This is the second sample photo in the Adventure Works photo application",
                    UserName = "RogerLengel",
                    //PhotoFile = getFileBytes("\\Images\\orchard.jpg"),
                    ImageMimeType = "image/jpeg",
                    AzurePath = storeInAzure("Images\\orchard.jpg"),
                    CreatedDate = DateTime.Today.AddDays(-14),
                    Location = "London, UK",
                    Latitude = "51.506321",
                    Longitude = "-0.12714"
                },
                new Photo {
                    Title = "Sample Photo 3",
                    Description = "This is the third sample photo in the Adventure Works photo application",
                    UserName = "AllisonBrown",
                    //PhotoFile = getFileBytes("\\Images\\path.jpg"),
                    ImageMimeType = "image/jpeg",
                    AzurePath = storeInAzure("Images\\path.jpg"),
                    CreatedDate = DateTime.Today.AddDays(-14),
                    Location = "Tokyo, Japan",
                    Latitude = "35.683208",
                    Longitude = "139.808945"
                }
            };
            photos.ForEach(s => context.Photos.Add(s));
            context.SaveChanges();

            //Create some comments
            var comments = new List<Comment>
            {
                new Comment {
                    PhotoID = 1,
                    UserName = "JamieStark",
                    Subject = "Sample Comment 1",
                    Body = "This is the first sample comment in the Adventure Works photo application"
                },
                new Comment {
                    PhotoID = 1,
                    UserName = "JimCorbin",
                    Subject = "Sample Comment 2",
                    Body = "This is the second sample comment in the Adventure Works photo application"
                },
                new Comment {
                    PhotoID = 3,
                    UserName = "RogerLengel",
                    Subject = "Sample Comment 3",
                    Body = "This is the third sample photo in the Adventure Works photo application"
                }
            };
            comments.ForEach(s => context.Comments.Add(s));
            context.SaveChanges();
        }

        private string storeInAzure(string ImageToUpload)
        {
            CloudStorageAccount storageAccount=
                CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnStr"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("photosharing");
            try
            {
                BlobRequestOptions requestOptions = new BlobRequestOptions() { RetryPolicy = new NoRetry() };
                container.CreateIfNotExistsAsync(requestOptions, null).Wait();

                BlobContainerPermissions permissions = container.GetPermissions();
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                container.SetPermissions(permissions);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageToUpload);
                blockBlob.Properties.ContentType = "image/jpeg";
                blockBlob.UploadFromFileAsync(HttpRuntime.AppDomainAppPath + ImageToUpload).Wait();
                return blockBlob.Uri.ToString();
            }
            catch (StorageException)
            {
                Trace.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }


        }

        //This gets a byte array for a file at the path specified
        //The path is relative to the route of the web site
        //It is used to seed images
        private byte[] getFileBytes(string path)
        {
            FileStream fileOnDisk = new FileStream(HttpRuntime.AppDomainAppPath + path, FileMode.Open);
            byte[] fileBytes;
            using (BinaryReader br = new BinaryReader(fileOnDisk))
            {
                fileBytes = br.ReadBytes((int)fileOnDisk.Length);
            }
            return fileBytes;
        }

    }
}