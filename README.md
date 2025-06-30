# Entain – Technical assignment

## 📝 Assignment Overview
Create a program to download multiple web pages asynchronously.

## ⚙️ Technical Implementation
Logic is placed in project WebsiteDownloader which could be extracted into separate nuget package and injected by the consumers. 
Currently there is a basic console app that simply creates an object and tries the download.

### 🚀 How to Run
App expects at least 2 arguments, 3rd one is optional:
1. destination of local folder where websites will be downloaded into files
2. path to text file containing URLs of the website (there is txt file in console project with 250 URLs to Entain pages. Actually 5 URLs are copied, and duplicates are allowed in order to simplify test of big number of websites)
3. number of partitions used during parallelisation - (optional since there is a default value, I included it since it might make sense to be passed by the consumer of the Downloader in some scenarios)