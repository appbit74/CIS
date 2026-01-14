using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using Microsoft.AspNetCore.Authorization;
using Ganss.Xss;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Generic;
using CIS.ViewModels; // 1. *** เพิ่ม Using นี้ ***

namespace CIS.Controllers
{
    [Authorize]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HtmlSanitizer _sanitizer;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public NewsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;

            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Add("img");
            _sanitizer.AllowedAttributes.Add("src");
            _sanitizer.AllowedSchemes.Add("data");
        }

        // GET: News
        public async Task<IActionResult> Index()
        {
            return View(await _context.NewsArticles.OrderByDescending(n => n.PublishedDate).ToListAsync());
        }

        // --- 2. *** [GET] Details (ผ่าตัดใหญ่!) *** ---
        [HttpGet] // (ระบุให้ชัดเจน)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // 3. ดึง "Model" (ของจริง) จาก DB (เหมือนเดิม)
            var newsArticle = await _context.NewsArticles
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (newsArticle == null) return NotFound();

            // 4. *** สร้าง "ViewModel" (กล่องป้อน View) ***
            var viewModel = new NewsDetailViewModel
            {
                News = newsArticle // 5. ยัด "ข่าว" (ตัวแม่) ลงไป
            };

            // 6. *** "ประมวลผล" ไฟล์แนบ (ใน Controller ที่เสถียร) ***
            if (newsArticle.Attachments != null)
            {
                foreach (var attachment in newsArticle.Attachments)
                {
                    // 7. *** นี่ไง! "Logic" ที่เราย้ายมา! ***
                    var ext = Path.GetExtension(attachment.StoredFilePath ?? "").ToLowerInvariant();

                    // 8. สร้าง "กล่องลูก"
                    var processedAttachment = new AttachmentViewModel
                    {
                        Id = attachment.Id,
                        OriginalFileName = attachment.OriginalFileName,
                        StoredFilePath = attachment.StoredFilePath,
                        FileSizeInBytes = attachment.FileSizeInBytes,
                        Extension = ext // 9. *** ยัด "ext" ที่ประมวลผลแล้วลงไป! ***
                    };

                    viewModel.ProcessedAttachments.Add(processedAttachment);
                }
            }

            // 10. *** ส่ง "ViewModel" (กล่องที่ประมวลผลแล้ว) ไปให้ View ***
            return View(viewModel);
        }

        // GET: News/Create
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public IActionResult Create()
        {
            var model = new NewsArticle { PublishedDate = DateTime.Now, IsPublished = true };
            return View(model);
        }

        // POST: News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> Create(
            [Bind("Id,Title,Content")] NewsArticle newsArticle,
            List<IFormFile> attachments,
            string submitButton)
        {
            newsArticle.PublishedDate = DateTime.Now;
            newsArticle.Author = User.Identity?.Name ?? "System";
            ModelState.Remove("Author");

            if (ModelState.IsValid)
            {
                if (submitButton == "Publish") newsArticle.IsPublished = true;
                else newsArticle.IsPublished = false;

                newsArticle.Content = _sanitizer.Sanitize(newsArticle.Content);
                _context.Add(newsArticle);
                await _context.SaveChangesAsync();

                if (attachments != null && attachments.Count > 0)
                {
                    await ProcessAndSaveAttachments(attachments, newsArticle.Id);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            return View(newsArticle);
        }

        // GET: News/Edit/5
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var newsArticle = await _context.NewsArticles
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);
            if (newsArticle == null) return NotFound();
            return View(newsArticle);
        }

        // POST: News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Content,PublishedDate,Author")] NewsArticle newsArticle,
            List<IFormFile> newAttachments,
            int[]? attachmentsToDelete,
            string submitButton)
        {
            if (id != newsArticle.Id) return NotFound();

            var articleToUpdate = await _context.NewsArticles
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (articleToUpdate == null) return NotFound();

            if (!ModelState.IsValid)
            {
                newsArticle.Attachments = articleToUpdate.Attachments;
                return View(newsArticle);
            }

            try
            {
                articleToUpdate.Title = newsArticle.Title;
                articleToUpdate.Content = _sanitizer.Sanitize(newsArticle.Content);
                articleToUpdate.PublishedDate = newsArticle.PublishedDate;
                if (submitButton == "Publish") articleToUpdate.IsPublished = true;
                else articleToUpdate.IsPublished = false;

                _context.Update(articleToUpdate);

                if (attachmentsToDelete != null && attachmentsToDelete.Length > 0)
                {
                    foreach (var attachmentId in attachmentsToDelete)
                    {
                        var attachmentToDelete = articleToUpdate.Attachments
                            .FirstOrDefault(a => a.Id == attachmentId);
                        if (attachmentToDelete != null)
                        {
                            DeleteFileFromServer(attachmentToDelete.StoredFilePath);
                            _context.NewsAttachments.Remove(attachmentToDelete);
                        }
                    }
                }

                if (newAttachments != null && newAttachments.Count > 0)
                {
                    await ProcessAndSaveAttachments(newAttachments, articleToUpdate.Id);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.NewsArticles.Any(e => e.Id == newsArticle.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var newsArticle = await _context.NewsArticles
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (newsArticle != null)
            {
                if (newsArticle.Attachments != null && newsArticle.Attachments.Any())
                {
                    foreach (var attachment in newsArticle.Attachments)
                    {
                        DeleteFileFromServer(attachment.StoredFilePath);
                    }
                }
                _context.NewsArticles.Remove(newsArticle);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /News/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> DeleteMultiple(int[] idsToDelete)
        {
            if (idsToDelete == null || idsToDelete.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var articles = await _context.NewsArticles
                .Include(n => n.Attachments)
                .Where(n => idsToDelete.Contains(n.Id))
                .ToListAsync();

            if (articles.Any())
            {
                foreach (var article in articles)
                {
                    if (article.Attachments != null)
                    {
                        foreach (var attachment in article.Attachments)
                        {
                            DeleteFileFromServer(attachment.StoredFilePath);
                        }
                    }
                }

                _context.NewsArticles.RemoveRange(articles);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // (Helper Functions: DeleteFileFromServer, ProcessAndSaveAttachments, UploadImage... ทั้งหมดเหมือนเดิม 100%)

        private void DeleteFileFromServer(string storedFilePath)
        {
            try
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string physicalPath = Path.Combine(webRootPath, storedFilePath.TrimStart('/', '\\'));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception) { /* (Log) */ }
        }

        private async Task ProcessAndSaveAttachments(List<IFormFile> files, int newsArticleId)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;
            string uploadPath = Path.Combine(webRootPath, "uploads", "attachments");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip" };
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(extension) && allowedExtensions.Contains(extension))
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine(uploadPath, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        var attachment = new NewsAttachment
                        {
                            OriginalFileName = Path.GetFileName(file.FileName),
                            StoredFilePath = $"/uploads/attachments/{uniqueFileName}",
                            FileType = file.ContentType,
                            FileSizeInBytes = file.Length,
                            NewsArticleId = newsArticleId
                        };
                        _context.NewsAttachments.Add(attachment);
                    }
                }
            }
        }

        [HttpPost]
        [Authorize(Roles = @"CRIMCAD\News_Admins")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type.");
            }
            string webRootPath = _webHostEnvironment.WebRootPath;
            string uploadPath = Path.Combine(webRootPath, "uploads", "news");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadPath, uniqueFileName);
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception) { return StatusCode(500, "Internal server error."); }
            string publicUrl = $"/uploads/news/{uniqueFileName}";
            return Json(new { url = publicUrl });
        }
    }
}