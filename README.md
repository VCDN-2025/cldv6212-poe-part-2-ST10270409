# CLDV6212 – POE Part 2 (ST10270409)

Deployed MVC: https://<your-mvc-app>.azurewebsites.net  
Function App: https://cloudpoepart2st10270409lg.azurewebsites.net

## What’s in this repo
- MVC: `FitHub.Web` (+ `/Part2` page to call Azure Functions)
- Functions: `Products_Create`, `Blobs_UploadFromUrl`, `Queue_Enqueue`, `WriteFileShare`

## Config (kept out of code)
- ConnectionStrings:AzureStorage (App Service)
- AzureFunctions:* URLs and AzureFunctions:Key (App Service)
