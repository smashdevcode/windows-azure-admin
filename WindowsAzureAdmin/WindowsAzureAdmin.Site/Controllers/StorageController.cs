using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WindowsAzureAdmin.Shared.Storage;

namespace WindowsAzureAdmin.Site.Controllers
{
    public class StorageController : Controller
    {
		public ActionResult Blobs()
		{
			// JCTODO add drop down list to the view to select the container
			return View(BlobStorage.GetBlobs("videos"));
		}
		public ActionResult BlobDelete(string fileName)
		{
			// JCTODO add drop down list to the view to select the container
			BlobStorage.DeleteBlob("videos", fileName);
			return RedirectToAction("Blobs");
		}
		public ActionResult Containers()
		{
			return View(BlobStorage.GetContainers());
		}
		public ActionResult ContainerDelete(string containerName)
		{
			BlobStorage.DeleteContainer(containerName);
			return RedirectToAction("Containers");
		}
	}
}
