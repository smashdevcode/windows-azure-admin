using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WindowsAzureAdmin.Shared.Storage;

namespace WindowsAzureAdmin.Shared.Media
{
	public class MediaServices : IDisposable
	{
		private CloudMediaContext _context = null;

		public MediaServices()
		{
			var accountName = CloudConfigurationManager.GetSetting("MediaServicesAccountName");
			var accountKey = CloudConfigurationManager.GetSetting("MediaServicesAccountKey");
			_context = new CloudMediaContext(accountName, accountKey);
		}

		#region Context Collections
		public List<IAsset> GetAssets()
		{
			return _context.Assets.ToList().OrderByDescending(a => a.Created).ToList();
		}
		public List<IContentKey> GetContentKeys()
		{
			return _context.ContentKeys.ToList().OrderByDescending(ck => ck.Created).ToList();
		}
		public List<IFileInfo> GetFiles()
		{
			return _context.Files.ToList().OrderByDescending(f => f.Created).ToList();
		}
		public List<IJob> GetJobs()
		{
			return _context.Jobs.ToList().OrderByDescending(j => j.Created).ToList();
		}
		public List<ILocator> GetLocators()
		{
			return _context.Locators.ToList().OrderBy(l => l.AssetId).OrderBy(l => l.ExpirationDateTime).ToList();
		}
		public List<IMediaProcessor> GetMediaProcessors()
		{
			return _context.MediaProcessors.ToList().OrderBy(mp => mp.Name).ToList();
		}
		#endregion
		#region Assets
		public IAsset GetAsset(string assetID)
		{
			var asset = (from a in _context.Assets
						 where a.Id == assetID
						 select a).FirstOrDefault();
			if (asset == null)
				throw new ApplicationException("Unknown asset: " + assetID);
			return asset;
		}
		public void DeleteAsset(string assetID)
		{
			var asset = GetAsset(assetID);
			if (asset != null)
			{
				foreach (var locator in asset.Locators)
					_context.Locators.Revoke(locator);

				//var numberOfContentKeys = asset.ContentKeys.Count();
				//for (int i = 0; i < numberOfContentKeys; i++)
				//	asset.ContentKeys.RemoveAt(i);
				foreach (var contentKey in asset.ContentKeys)
					_context.ContentKeys.Delete(contentKey);

				_context.Assets.Delete(asset);
			}
		}
		public string GetAssetSasUrl(IAsset asset, string fileExtension)
		{
			return GetAssetSasUrl(asset, fileExtension, new TimeSpan(1, 0, 0));
		}
		public string GetAssetSasUrl(IAsset asset, string fileExtension, TimeSpan accessPolicyTimeout)
		{
			var file = (from f in asset.Files
						where f.Name.EndsWith(fileExtension)
						select f).FirstOrDefault();
			if (file != null)
				return GetAssetSasUrl(asset, file, accessPolicyTimeout);
			else
				return null;
		}
		public string GetAssetSasUrl(IAsset asset, IFileInfo file)
		{
			return GetAssetSasUrl(asset, file, new TimeSpan(1, 0, 0));
		}
		public string GetAssetSasUrl(IAsset asset, IFileInfo file, TimeSpan accessPolicyTimeout)
		{
			// check to see if a locator is already available
			// (that doesn't expire for another 30 minutes)
			var locator = (from l in asset.Locators
						   orderby l.ExpirationDateTime descending
						   where l.ExpirationDateTime > DateTime.UtcNow.AddMinutes(30)
						   select l).FirstOrDefault();
			if (locator == null)
			{
				// create a policy for the asset
				IAccessPolicy readPolicy = _context.AccessPolicies.Create("ReadPolicy", accessPolicyTimeout, AccessPermissions.Read);
				locator = _context.Locators.CreateSasLocator(asset, readPolicy, DateTime.UtcNow.AddMinutes(-5));
			}
			Trace.WriteLine("Locator path: " + locator.Path);

			// now take the locator path, add the file name, and build a complete SAS URL to browse to the asset
			var uriBuilder = new UriBuilder(locator.Path);
			uriBuilder.Path += "/" + file.Name;
			Trace.WriteLine("Full URL to file: " + uriBuilder.Uri.AbsoluteUri);

			// return the url
			return uriBuilder.Uri.AbsoluteUri;
		}
		#endregion
		#region Content Keys
		public void DeleteContentKey(string contentKeyID)
		{
			var contentKey = (from ck in _context.ContentKeys
							  where ck.Id == contentKeyID
							  select ck).FirstOrDefault();
			if (contentKey == null)
				throw new ApplicationException("Unknown content key: " + contentKeyID);
			_context.ContentKeys.Delete(contentKey);
		}
		#endregion
		#region Jobs
		public IJob GetJob(string jobID)
		{
			var job = (from j in _context.Jobs
					   where j.Id == jobID
					   select j).FirstOrDefault();
			if (job == null)
				throw new ApplicationException("Unknown job: " + jobID);
			return job;
		}
		public void CancelJob(string jobID)
		{
			var job = GetJob(jobID);
			if (job.State == JobState.Processing || job.State == JobState.Queued || job.State == JobState.Scheduled)
				job.Cancel();
		}
		public void DeleteJob(string jobID)
		{
			var job = GetJob(jobID);
			job.Delete();
		}
		public string CreateEncodingJob(string jobIdentifer, string containerName, string fileName)
		{
			// create an empty asset
			IAsset asset = _context.Assets.CreateEmptyAsset(
				string.Format("Asset_", jobIdentifer), AssetCreationOptions.None);

			// create a locator to get the SAS (shared access signature) URL
			IAccessPolicy writePolicy = _context.AccessPolicies.Create("WriteListPolicy", TimeSpan.FromMinutes(30),
				AccessPermissions.Write | AccessPermissions.List);
			ILocator destinationLocator = _context.Locators.CreateSasLocator(asset, writePolicy, DateTime.UtcNow.AddMinutes(-5));

			// create the reference to the destination container
			var destinationFileUrl = new Uri(destinationLocator.Path);
			var destinationContainerName = destinationFileUrl.Segments[1];

			// get and validate the source blob
			var sourceFileBlob = BlobStorage.GetBlob(containerName, fileName);
			sourceFileBlob.FetchAttributes();
			var sourceLength = sourceFileBlob.Properties.Length;
			Debug.Assert(sourceLength > 0);

			// if we got here then we can assume the source is valid and accessible

			// create destination blob for copy, in this case, we choose to rename the file
			var destinationFileBlob = BlobStorage.GetBlob(destinationContainerName, fileName);
			destinationFileBlob.CopyFromBlob(sourceFileBlob);  // will fail here if project references are bad (the are lazy loaded)

			// check destination blob
			destinationFileBlob.FetchAttributes();
			System.Diagnostics.Debug.Assert(sourceFileBlob.Properties.Length == sourceLength);

			// if we got here then the copy worked

			// publish the asset
			asset.Publish();

			// refresh the asset
			asset = GetAsset(asset.Id);

			// declare a new job
			var job = _context.Jobs.Create(string.Format("EncodingJob_{0}", jobIdentifer));

			// get a media processor reference, and pass to it the name of the 
			// processor to use for the specific task
			var processor = GetMediaProcessorByName("Windows Azure Media Encoder");

			// create a task with the encoding details, using a string preset
			var task = job.Tasks.AddNew(
				string.Format("EncodingTask_MP4_{0}", jobIdentifer),
				processor,
				"H.264 256k DSL CBR",
				TaskCreationOptions.None);

			// Specify the input asset to be encoded
			task.InputMediaAssets.Add(asset);

			// add an output asset to contain the results of the job
			task.OutputMediaAssets.AddNew(string.Format("{0} H264", fileName),
				true, AssetCreationOptions.None);

			// submit the job
			job.Submit();

			// return the job id
			return job.Id;
		}
		#endregion
		#region Locators
		public void RevokeLocator(string locatorID)
		{
			var locator = (from l in _context.Locators
						   where l.Id == locatorID
						   select l).FirstOrDefault();
			if (locator == null)
				throw new ApplicationException("Unknown locator: " + locatorID);
			_context.Locators.Revoke(locator);
		}
		#endregion
		#region MediaProcessors
		private IMediaProcessor GetMediaProcessorByName(string mediaProcessorName)
		{
			var mediaProcessor = (from p in _context.MediaProcessors
								  where p.Name == mediaProcessorName
								  select p).FirstOrDefault();
			if (mediaProcessor == null)
				throw new ArgumentException(string.Format("Unknown media processor: {0}", mediaProcessorName));
			return mediaProcessor;
		}
		#endregion
		//private void CheckJobProgress(string jobID)
		//{
		//	// flag to indicate when job state is finished
		//	bool jobCompleted = false;
		//	// expected polling interval in milliseconds
		//	// adjust this interval as needed based on estimated job completion times
		//	const int jobProgressInterval = 20000;

		//	while (!jobCompleted)
		//	{
		//		// get an updated reference to the job in case 
		//		// reference gets 'stale' while thread waits
		//		IJob theJob = GetJob(jobID);

		//		// check job and report state
		//		switch (theJob.State)
		//		{
		//			case JobState.Finished:
		//				jobCompleted = true;
		//				Trace.WriteLine("Job finished...");
		//				break;
		//			case JobState.Queued:
		//			case JobState.Scheduled:
		//			case JobState.Processing:
		//				Trace.WriteLine("Job state: " + theJob.State);
		//				Trace.WriteLine("Please wait...");
		//				break;
		//			case JobState.Error:
		//				throw new ApplicationException("Encoding task failed.");
		//			default:
		//				Trace.WriteLine(theJob.State.ToString());
		//				break;
		//		}

		//		// wait for the specified job interval before checking state again
		//		Thread.Sleep(jobProgressInterval);
		//	}
		//}

		public void Dispose()
		{
			_context.DetachAll();
		}
	}
}
