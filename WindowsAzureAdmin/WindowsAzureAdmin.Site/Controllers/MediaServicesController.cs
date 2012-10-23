using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WindowsAzureAdmin.Shared.Media;

namespace WindowsAzureAdmin.Site.Controllers
{
    public class MediaServicesController : Controller
    {
		public MediaServices MediaServices { get; set; }

		public MediaServicesController()
		{
			this.MediaServices = new MediaServices();
		}

		public ActionResult Assets()
		{
			var assets = this.MediaServices.GetAssets();
			return View(assets);
		}
		public ActionResult AssetDetails(string assetID)
		{
			var asset = this.MediaServices.GetAsset(assetID);
			return View(asset);
		}
		public ActionResult AssetDelete(string assetID)
		{
			this.MediaServices.DeleteAsset(assetID);
			return RedirectToAction("Assets");
		}
		public ActionResult AssetLocatorDelete(string assetID, string locatorID)
		{
			this.MediaServices.RevokeLocator(locatorID);
			return RedirectToAction("AssetDetails", new { assetid = assetID });
		}
		public ActionResult ContentKeys()
		{
			var contentKeys = this.MediaServices.GetContentKeys();
			return View(contentKeys);
		}
		public ActionResult ContentKeyDelete(string contentKeyID)
		{
			this.MediaServices.DeleteContentKey(contentKeyID);
			return RedirectToAction("ContentKeys");
		}
		public ActionResult Files()
		{
			var files = this.MediaServices.GetFiles();
			return View(files);
		}
		public ActionResult Jobs()
		{
			var jobs = this.MediaServices.GetJobs();
			return View(jobs);
		}
		public ActionResult JobDetails(string jobID)
		{
			var job = this.MediaServices.GetJob(jobID);
			return View(job);
		}
		public ActionResult JobCancel(string jobID)
		{
			this.MediaServices.CancelJob(jobID);
			return RedirectToAction("Jobs");
		}
		public ActionResult JobDelete(string jobID)
		{
			this.MediaServices.DeleteJob(jobID);
			return RedirectToAction("Jobs");
		}
		public ActionResult TaskDetails(string jobID, string taskID)
		{
			var job = this.MediaServices.GetJob(jobID);
			var task = job.Tasks.Where(t => t.Id == taskID).First();
			ViewBag.JobID = jobID;
			return View(task);
		}
		public ActionResult Locators()
		{
			var locators = this.MediaServices.GetLocators();
			return View(locators);
		}
		public ActionResult LocatorDelete(string locatorID)
		{
			this.MediaServices.RevokeLocator(locatorID);
			return RedirectToAction("Locators");
		}
		public ActionResult MediaProcessors()
		{
			var mediaProcessors = this.MediaServices.GetMediaProcessors();
			return View(mediaProcessors);
		}
	}
}
