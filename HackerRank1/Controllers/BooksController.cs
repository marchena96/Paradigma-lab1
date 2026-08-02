using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LibraryService.WebAPI.Data;
using LibraryService.WebAPI.DTO;
using LibraryService.WebAPI.Services;

namespace LibraryService.WebAPI.Controllers
{
    [ApiController]
    [Route("api/libraries/{libraryId}/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly ILibrariesService _librariesService;
        private readonly IBooksService _booksService;

        public BooksController(IBooksService booksService, ILibrariesService librariesService)
        {
            _librariesService = librariesService;
            _booksService = booksService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int libraryId)
        {
            var library = (await _librariesService.Get(new[] { libraryId })).FirstOrDefault();
            if (library == null)
                return NotFound();

            var books = await _booksService.Get(libraryId, null);
            return Ok(books);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int libraryId, BookForm bookForm)
        {
            var library = (await _librariesService.Get(new[] { libraryId })).FirstOrDefault();
            if (library == null)
                return NotFound();

            var book = new Book
            {
                Name = bookForm.Name,
                Category = bookForm.Category ?? string.Empty,
                LibraryId = libraryId
            };

            var createdBook = await _booksService.Add(book);
            return CreatedAtAction(nameof(GetAll), new { libraryId = createdBook.LibraryId }, createdBook);
        }
    }
}
