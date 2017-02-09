using PhotoSharingApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PhotoSharingApplication.Controllers
{
    public class FirstController : ApiController
    {
        [HttpGet]
        public IEnumerable<Photo> GetAllPhotos()
        {
            PhotoSharingContext psc = new PhotoSharingContext();
            return psc.Photos.ToList<Photo>();
        

        }
        [HttpDelete]
        public HttpStatusCode DeletePhoto(int id)
        {
            PhotoSharingContext psc = new PhotoSharingContext();
            var photo = psc.Photos.Where(p => p.PhotoID == id).FirstOrDefault();

           if (photo==null)
            { return HttpStatusCode.NotFound; }
            else
            {
                psc.Photos.Remove(photo);
                psc.SaveChanges();
                return HttpStatusCode.OK;
                
            }
        }

        public class Customer
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

    }
}
