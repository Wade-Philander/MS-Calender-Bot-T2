using Azure.Storage.Blobs;
using EchoBotWN.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EchoBotWN.Excel
{
    public class excel
    {
        public static async Task<List<eventModel>> getEvents()
        {

            List<eventModel> allEvents = new List<eventModel>();

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=botwnv2container;AccountKey=9R8QcJfM8wZNBoBLVxrfe2IKGmQqyN5dKCVIPjBjkhIbNiYQeL1u+1uhR3wuOJPlsscexBKWU0pgXPhHcmFgow==;EndpointSuffix=core.windows.net";

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("main-container");
            BlobClient blobClient = containerClient.GetBlobClient("botwnv2-csv.csv");


            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        var lineArray = line.Split(";");
                        if(lineArray.Length > 3)
                        allEvents.Add(new eventModel(lineArray[0], lineArray[1], lineArray[2],lineArray[3]));
                        Console.WriteLine(line);
                    }
                }
            }
            return allEvents;
        }


        public static async Task<bool> addEvent(eventModel newEvent)
        {
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=botwnv2container;AccountKey=9R8QcJfM8wZNBoBLVxrfe2IKGmQqyN5dKCVIPjBjkhIbNiYQeL1u+1uhR3wuOJPlsscexBKWU0pgXPhHcmFgow==;EndpointSuffix=core.windows.net";
            int id = 0;
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("main-container");
            BlobClient blobClient = containerClient.GetBlobClient("botwnv2-csv.csv");
            var csv = new StringBuilder();
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using (var streamReader = new StreamReader(response.Value.Content))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        id++;
                        csv.AppendLine(line);
                        Console.WriteLine(line);
                    }
                }

                FileInfo info = new FileInfo(@"./data/botwnv2-csv.csv"); // Change to your directory



                csv.AppendLine($"{id};{newEvent.subject};{newEvent.message};{newEvent.date}");

                /*   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                   using (var package = new ExcelPackage())
                   {

                       var sheet = package.Workbook.Worksheets.Add("Audit Log");
                       var sheetRow = 2;

                   */
                string localPath = "./data/";
                string fileName = "botwnv2-csv.csv";
                string localFilePath = Path.Combine(localPath, fileName);


                Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);

                // Open the file and upload its data
                File.WriteAllText(localFilePath, csv.ToString());


                FileStream uploadFileStream = File.OpenRead(localFilePath);
                await blobClient.UploadAsync(uploadFileStream, true);
                uploadFileStream.Close();
            }
            return true;
        }
    }
}
