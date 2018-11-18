﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;

namespace google_drive_sample
{
    public class GoogleDriveHelper
    {
        private const string CredentialPath = "../credentials";
        private string[] Scopes = { 
                                      DriveService.Scope.Drive, 
                                      DriveService.Scope.DriveFile, 
                                      DriveService.Scope.DriveMetadata 
                                  };
        private const string ApplicationName = "google-drive-sample";

        private UserCredential _userCredential;
        public DriveService _driveService;

        public GoogleDriveHelper()
        {
            GetAuth();
        }

        public Google.Apis.Drive.v2.Data.File GetFileById(string id)
        {
            if (id != null)
            {
                FilesResource.GetRequest file = _driveService.Files.Get(id);
                return file.Execute();
            }
            else return new Google.Apis.Drive.v2.Data.File();
        }

        public Google.Apis.Drive.v2.Data.File CreateDirectory(string parent_path, string file_name)
        {
            Google.Apis.Drive.v2.Data.File body = new Google.Apis.Drive.v2.Data.File();
            body.Title = file_name;
            body.MimeType = "application/vnd.google-apps.folder";
            body.Parents = new List<ParentReference>() 
            {
                new ParentReference() { Id = GetIdByPath(parent_path) } 
            };

            return _driveService.Files.Insert(body).Execute();
        }
        public Google.Apis.Drive.v2.Data.File CreateDirectory(string path)
        {
            string file_name = path.Substring(path.LastIndexOf('/'), path.Length);
            string parent_path = path.Substring(0, path.LastIndexOf('/'));

            Google.Apis.Drive.v2.Data.File body = new Google.Apis.Drive.v2.Data.File();
            body.Title = file_name;
            body.MimeType = "application/vnd.google-apps.folder";
            body.Parents = new List<ParentReference>() 
            {
                new ParentReference() { Id = GetIdByPath(parent_path) } 
            };

            return _driveService.Files.Insert(body).Execute();
        }
        public void GetAuth()
        {
            using (var stream = new FileStream("../../credentials/google_secret.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(CredentialPath, true));
                _userCredential = credentials.Result;
                if (credentials.IsCanceled || credentials.IsFaulted)
                    throw new Exception("cannot connect");

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _userCredential,
                    ApplicationName = ApplicationName,
                });
            }
        }

        public string GetRecursiveParent(string path, IList<ParentReference> parent, int idx)
        {
            string[] path_list = path.Split('/');
            FilesResource.GetRequest parent_req = _driveService.Files.Get(parent[0].Id);
            Google.Apis.Drive.v2.Data.File parent_file = parent_req.Execute();

            if (parent[0].IsRoot.Value)
                return parent[0].Id;
            if (parent_file.Title == path_list[idx] && !parent[0].IsRoot.Value)
                return GetRecursiveParent(path, parent_file.Parents, idx - 1);
            else
                return null;
        }

        public string GetIdByPath(string path)
        {
            if (path == "/" || path == "")
                return "root";

            string[] path_list = path.Split('/');
            List<Google.Apis.Drive.v2.Data.File> file_search_list = new List<Google.Apis.Drive.v2.Data.File>();

            FilesResource.ListRequest req = _driveService.Files.List();
            do
            {
                req.Q = "title='" + path_list.Last<string>() + "'";
                FileList file_search = req.Execute();
                file_search_list.AddRange(file_search.Items);
            } while (!String.IsNullOrEmpty(req.PageToken));

            if (file_search_list.Count == 1)
            {
                return file_search_list.First<Google.Apis.Drive.v2.Data.File>().Id;
            }
            else
            {
                int last_idx = path_list.Length - 2;
                Google.Apis.Drive.v2.Data.File ret = new Google.Apis.Drive.v2.Data.File();
                foreach (Google.Apis.Drive.v2.Data.File f in file_search_list)
                {
                    if (GetRecursiveParent(path, f.Parents, last_idx) != null)
                    {
                        ret = f;
                        break;
                    }
                }

                return ret.Id;
            }
        }

        public void GetFile(string file_name)
        {
            FilesResource.ListRequest req = _driveService.Files.List();

            do
            {
                req.Q = "title='" + file_name + "'";
                FileList file_search = req.Execute();
                foreach (Google.Apis.Drive.v2.Data.File a in file_search.Items)
                {
                    System.Windows.Forms.MessageBox.Show(a.Id);
                }
            } while (!String.IsNullOrEmpty(req.PageToken));
        }

        public List<Google.Apis.Drive.v2.Data.File> GetChildren(string dir_name)
        {
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();

            FilesResource.ListRequest req = _driveService.Files.List();
            req.Q = "title='" + dir_name + "'";
            FileList children_list = req.Execute();
            ChildrenResource.ListRequest child_req = _driveService.Children.List(children_list.Items[0].Id);
            ChildList ch = child_req.Execute();

            foreach (ChildReference a in ch.Items)
            {
                FilesResource.GetRequest get_file = _driveService.Files.Get(a.Id);
                Google.Apis.Drive.v2.Data.File file_obj = get_file.Execute();
                result.Add(file_obj);
            }

            return result;
        }

        public List<Google.Apis.Drive.v2.Data.File> GetChildrenById(string id)
        {
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();

            FilesResource.ListRequest req = _driveService.Files.List();
            req.Q = "trashed=false";
            req.Q += string.Format(" and '{0}' in parents", id);

            FileList files = req.Execute();
            result = files.Items.ToList<Google.Apis.Drive.v2.Data.File>();

            return result;
        }

        public List<Google.Apis.Drive.v2.Data.File> GetRoot()
        {
            List<Google.Apis.Drive.v2.Data.File> result = new List<Google.Apis.Drive.v2.Data.File>();

            ChildrenResource.ListRequest child_req = _driveService.Children.List("root");
            ChildList ch = child_req.Execute();

            foreach (ChildReference a in ch.Items)
            {
                FilesResource.GetRequest get_file = _driveService.Files.Get(a.Id);
                Google.Apis.Drive.v2.Data.File file_obj = get_file.Execute();
                result.Add(file_obj);
            }

            return result;
        }


        public  void SaveStream(System.IO.MemoryStream stream, string saveTo)
        {
            using (System.IO.FileStream file = new System.IO.FileStream(saveTo, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                stream.WriteTo(file);
            }
        }

        public  void DownloadFile( Google.Apis.Drive.v2.Data.File file, string saveTo)
        {

            var request = _driveService.Files.Get(file.Id);
            var stream = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case Google.Apis.Download.DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream, saveTo);
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            request.Download(stream);

        }




        public System.IO.MemoryStream DownloadFile1(Google.Apis.Drive.v2.Data.File file, string saveTo)
        {

            var request = _driveService.Files.Get(file.Id);
            var stream = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case Google.Apis.Download.DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream, saveTo);
                           
                            break;
                        }
                    case Google.Apis.Download.DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            request.Download(stream);
            return stream;

            

        }




        public bool IsDirectory(Google.Apis.Drive.v2.Data.File file)
        {
            if (!file.Copyable.HasValue)
            {
                return false;
            }

            return (!file.Copyable.Value && file.MimeType == "application/vnd.google-apps.folder");
        }
    }
}
