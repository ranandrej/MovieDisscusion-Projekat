using Common;
using Common.DTOs;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MovieDiscussionService.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private CloudTable Users => Storage.GetTable("Users");

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]

        public ActionResult Register(RegisterDTO dto, HttpPostedFileBase PhotoFile)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var emailLower = dto.Email?.ToLowerInvariant();

            var existing = Users.Execute(
                TableOperation.Retrieve<UserEntity>("User", emailLower)
            ).Result as UserEntity;

            if (existing != null)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(dto);
            }

            string photoUrl = dto.PhotoUrl; // fallback ako neko nalepi direktno URL

            // Ako korisnik uploaduje fajl
            if (PhotoFile != null && PhotoFile.ContentLength > 0)
            {
                if (PhotoFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "The file size exceeds the allowed limit (5MB).");
                    return View(dto);
                }

                try
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);

                    // Uzmi kontejner (koristimo tvoj Storage.cs)
                    var containerName = ConfigurationManager.AppSettings["UserPhotosContainer"] ?? "user-photos";
                    var container = Storage.GetContainer(containerName);

                    var blob = container.GetBlockBlobReference(fileName);
                    blob.Properties.ContentType = PhotoFile.ContentType;
                    blob.UploadFromStream(PhotoFile.InputStream);

                    photoUrl = blob.Uri.AbsoluteUri;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error uploading photo: " + ex.Message);
                    return View(dto);
                }
            }

            var user = new UserEntity(emailLower)
            {
                FullName = dto.FullName,
                Gender = dto.Gender,
                Country = dto.Country,
                City = dto.City,
                Address = dto.Address,
                PasswordHash = HashPassword(dto.Password),
                PhotoUrl = photoUrl,
                IsAuthorVerified = false
            };

            Users.Execute(TableOperation.Insert(user));

            Session["email"] = emailLower;
            return RedirectToAction("Index", "Discussion");
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginDTO dto)
        {
            // Ako polja nisu validna (prazna ili pogrešan format emaila)
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            // Sigurnosna provera - email uvek u lowercase
            var user = Users.Execute(
                TableOperation.Retrieve<UserEntity>("User", dto.Email.ToLower())
            ).Result as UserEntity;

            if (user == null || user.PasswordHash != HashPassword(dto.Password))
            {
                ModelState.AddModelError("", "Invalid credentials.");
                return View(dto);
            }

            Session["email"] = user.RowKey;
            return RedirectToAction("Index", "Discussion");
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        [HttpGet]
        public ActionResult EditProfile()
        {
            var email = Session["email"]?.ToString();
            if (email == null) return RedirectToAction("Login");

            var user = Users.Execute(TableOperation.Retrieve<UserEntity>("User", email)).Result as UserEntity;
            if (user == null) return RedirectToAction("Login");

            var dto = new RegisterDTO
            {
                FullName = user.FullName,
                Gender = user.Gender,
                Country = user.Country,
                City = user.City,
                Address = user.Address,
                Email = user.RowKey,
                PhotoUrl = user.PhotoUrl   // ⬅ OVO je ključno
            };

            return View(dto);
        }

        [HttpPost]
        public ActionResult EditProfile(RegisterDTO dto, HttpPostedFileBase PhotoFile)
        {
            var email = Session["email"]?.ToString();
            if (email == null) return RedirectToAction("Login");

            var user = Users.Execute(TableOperation.Retrieve<UserEntity>("User", email)).Result as UserEntity;
            if (user == null) return RedirectToAction("Login");

            string photoUrl = user.PhotoUrl;

            if (PhotoFile != null && PhotoFile.ContentLength > 0)
            {
                if (PhotoFile.ContentLength > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "The file size exceeds the allowed limit (5MB).");
                    return View(dto);
                }

                try
                {
                    // obriši staru sliku
                    if (!string.IsNullOrEmpty(user.PhotoUrl))
                    {
                        var oldUri = new Uri(user.PhotoUrl);
                        var oldBlobName = Path.GetFileName(oldUri.LocalPath);
                        var container = Storage.GetContainer(ConfigurationManager.AppSettings["UserPhotosContainer"] ?? "user-photos");
                        var oldBlob = container.GetBlockBlobReference(oldBlobName);
                        oldBlob.DeleteIfExists();
                    }

                    // upload nove
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                    var newContainer = Storage.GetContainer(ConfigurationManager.AppSettings["UserPhotosContainer"] ?? "user-photos");
                    var newBlob = newContainer.GetBlockBlobReference(fileName);
                    newBlob.UploadFromStream(PhotoFile.InputStream);

                    photoUrl = newBlob.Uri.AbsoluteUri;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error uploading photo: " + ex.Message);
                    return View(dto);
                }
            }

            // ažuriraj user-a
            user.FullName = dto.FullName;
            user.Gender = dto.Gender;
            user.Country = dto.Country;
            user.City = dto.City;
            user.Address = dto.Address;
            user.PhotoUrl = photoUrl;

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = HashPassword(dto.Password);

            Users.Execute(TableOperation.Replace(user));

            return RedirectToAction("Index", "Discussion");
        }
    }
}