using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Globalization;
using PhotoSharingApplication.Models;
using PhotoSharingApplication.LocationCheckService;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Configuration;
using System.Diagnostics;

namespace PhotoSharingApplication.Controllers
{
    [HandleError(View = "Error")]
    [ValueReporter]
    public class PhotoController : Controller
    {
        private IPhotoSharingContext context;

        //Constructors
        public PhotoController()
        {
            context = new PhotoSharingContext();
        }

        public PhotoController(IPhotoSharingContext Context)
        {
            context = Context;
        }

        //
        // GET: /Photo/
        public ActionResult Index()
        {
            return View("Index");
        }

        [ChildActionOnly]
        public ActionResult _PhotoGallery(int number = 0)
        {
            List<Photo> photos;

            if (number == 0)
            {
                photos = context.Photos.ToList();
            }
            else
            {
                photos = (from p in context.Photos
                          orderby p.CreatedDate descending
                          select p).Take(number).ToList();
            }

            return PartialView("_PhotoGallery", photos);
        }

        public ActionResult Display(int id)
        {
            Photo photo = context.FindPhotoById(id);
            if (photo == null)
            {
                return HttpNotFound();
            }
            return View("Display", photo);
        }

        public ActionResult DisplayByTitle(string title)
        {
            Photo photo = context.FindPhotoByTitle(title);
            if (photo == null)
            {
                return HttpNotFound();
            }
            return View("Display", photo);
        }

        [Authorize]
        public ActionResult Create()
        {
            Photo newPhoto = new Photo();
            newPhoto.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name);
            newPhoto.CreatedDate = DateTime.Today;
            return View("Create", newPhoto);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Create(Photo photo, HttpPostedFileBase image)
        {
            photo.UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(User.Identity.Name);
            photo.CreatedDate = DateTime.Today;
            if (photo.Location != "")
            {
                string stringLatLong = CheckLocation(photo.Location);
                if (stringLatLong.StartsWith("Success"))
                {
                    char[] splitChars = {':'};
                    string[] coordinates = stringLatLong.Split(splitChars);
                    photo.Latitude = coordinates[1];
                    photo.Longitude = coordinates[2];
                }
            }
            if (!ModelState.IsValid)
            {
                return View("Create", photo);
            }
            else
            {
                if (image != null)
                {
                    photo.ImageMimeType = image.ContentType;
                    photo.PhotoFile = new byte[image.ContentLength];
                    photo.AzurePath = uploadFromStream(image.InputStream,image.FileName);
                }
                context.Add<Photo>(photo);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
        }

        private string uploadFromStream(Stream inputStream,string filename)
        {
            CloudStorageAccount storageAccount =
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

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
                blockBlob.Properties.ContentType = "image/jpeg";
                blockBlob.UploadFromStream(inputStream);
                return blockBlob.Uri.ToString();
            }
            catch (StorageException)
            {
                Trace.WriteLine("If you are running with the default connection string, please make sure you have started the storage emulator. Press the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }
        }

        [Authorize]
        public ActionResult Delete(int id)
        {
            Photo photo = context.FindPhotoById(id);
            if (photo == null)
            {
                return HttpNotFound();
            }
            return View("Delete", photo);
        }

        [Authorize]
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Photo photo = context.FindPhotoById(id);
            context.Delete<Photo>(photo);
            context.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult GetImage(int id)
        {
            Photo photo = context.FindPhotoById(id);
            if (photo != null)
            {
                return Content(photo.AzurePath);
            }
            else
            {
                return null;
            }
        }

        public ActionResult SlideShow()
        {
            return View("SlideShow", context.Photos.ToList());
        }

        public ActionResult FavoritesSlideshow()
        {
            List<Photo> favPhotos = new List<Photo>();
            List<int> favoriteIds = Session["Favorites"] as List<int>;
            if (favoriteIds == null)
            {
                favoriteIds = new List<int>();
            }
            Photo currentPhoto;

            foreach (int favID in favoriteIds)
            {
                currentPhoto = context.FindPhotoById(favID);
                if (currentPhoto != null)
                {
                    favPhotos.Add(currentPhoto);
                }
            }

            return View("SlideShow", favPhotos);
        }

        public ContentResult AddFavorite(int PhotoID)
        {
            List<int> favorites = Session["Favorites"] as List<int>;
            if (favorites == null)
            {
                favorites = new List<int>();
            }
            favorites.Add(PhotoID);
            Session["Favorites"] = favorites;
            return Content("The picture has been added to your favorites", "text/plain", System.Text.Encoding.Default);
        }

        private string CheckLocation(string location)
        {
            LocationCheckServiceClient client = null;
            string response = "";
            try
            {
                client = new LocationCheckServiceClient();
                response = client.GetLocation(location);
            }
            catch (Exception e)
            {
                response = "Error: " + e.Message;
            }
            return response;
        }

        public ViewResult Map()
        {
            return View();
        }

        [Authorize]
        public ActionResult Chat(int id)
        {
            Photo photo = context.FindPhotoById(id);
            if (photo == null)
            {
                return HttpNotFound();
            }
            return View("Chat", photo);
        }
    }
}
