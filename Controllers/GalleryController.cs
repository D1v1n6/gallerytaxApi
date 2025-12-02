using Microsoft.AspNetCore.Mvc;
using Project.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using PhotoGallery.Models;

namespace Project.Controllers
{
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GalleryController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Gallery
        public IActionResult Index()
        {
            var images = _context.Images.OrderByDescending(i => i.UploadDate).ToList();
            return View(images);
        }

        // GET: Gallery/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Gallery/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImageModel model, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string fileName = $"{Path.GetFileNameWithoutExtension(imageFile.FileName)}_{DateTime.Now.Ticks}{Path.GetExtension(imageFile.FileName)}";
                string filePath = Path.Combine(uploadDir, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                model.ImagePath = "/uploads/" + fileName;

                _context.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Please select an image file.");
            return View(model);
        }

        // ------------------------------
        //        API ENDPOINTS
        // ------------------------------

        /// <summary>
        /// Returns all images in the gallery.
        /// </summary>
        [HttpGet("api/gallery")]
        public IActionResult GetImagesApi()
        {
            var images = _context.Images
                .OrderByDescending(i => i.UploadDate)
                .ToList();

            return Ok(images);
        }

        /// <summary>
        /// Returns a single image by ID.
        /// </summary>
        [HttpGet("api/gallery/{id}")]
        public IActionResult GetImageApi(int id)
        {
            var image = _context.Images.Find(id);

            if (image == null)
                return NotFound();

            return Ok(image);
        }

        /// <summary>
        /// Upload an image via API.
        /// </summary>
        [HttpPost("api/gallery/upload")]
        public async Task<IActionResult> UploadApi(IFormFile imageFile, [FromForm] string title, [FromForm] string description)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image file uploaded.");

            string uploadDir = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            string fileName = $"{Path.GetFileNameWithoutExtension(imageFile.FileName)}_{DateTime.Now.Ticks}{Path.GetExtension(imageFile.FileName)}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            var image = new ImageModel
            {
                Title = title,
                Description = description,
                ImagePath = "/uploads/" + fileName,
                UploadDate = DateTime.Now
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Uploaded successfully",
                image
            });
        }

        /// <summary>
        /// Deletes an image by ID.
        /// </summary>
        [HttpDelete("api/gallery/{id}")]
        public async Task<IActionResult> DeleteImageApi(int id)
        {
            var img = await _context.Images.FindAsync(id);
            if (img == null)
                return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.Images.Remove(img);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted successfully" });
        }
    }
}
