using _301271988_chauhanpachchigar__Lab3.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _301271988_chauhanpachchigar__Lab3.Controllers
{
    [Authorize]
    public class MoviesController : Controller
    {
        private const string _movieTableName = "bingewatch-movies";
        private const string _bucketName = "assignment3-movies";
        private const string _commentsTableName = "bingewatch-comments";
        private readonly Helper _helper;

        public MoviesController(Helper helper)
        {
            _helper = helper;
        }
        // GET: /Movies/
        public async Task<IActionResult> Index(string genre = null, int minRating = 0)
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            var search = table.Scan(new ScanFilter());

            if (!string.IsNullOrEmpty(genre))
            {
                search.FilterExpression = new Expression
                {
                    ExpressionStatement = "Genre = :genreVal",
                    ExpressionAttributeValues = { { ":genreVal", genre } }
                };
            }

            if (minRating > 0)
            {
                search.FilterExpression = new Expression
                {
                    ExpressionStatement = "Rating >= :minRatingVal",
                    ExpressionAttributeValues = { { ":minRatingVal", minRating } }
                };
            }

            var documentList = await search.GetRemainingAsync();
            var movies = documentList.Select(doc => new Movie
            {
                MovieId = doc["MovieId"],
                Title = doc["Title"],
                Genre = doc["Genre"],
                Director = doc["Director"],
                ReleaseTime = doc["ReleaseTime"].AsDateTime(),
                UploadedByUserId = doc["UploadedByUserId"],
                Rating = doc["Rating"].AsDouble(),
                S3Url = doc["S3Url"]
            }).ToList();
            
            return View(movies);
        }

        // GET: /Movies/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            var movieDocument = await table.GetItemAsync(id); // Use id here

            if (movieDocument == null)
                return NotFound();

            // Map Document to Movie
            var movie = new Movie
            {
                MovieId = movieDocument["MovieId"],
                Title = movieDocument["Title"],
                Genre = movieDocument["Genre"],
                Director = movieDocument["Director"],
                ReleaseTime = movieDocument["ReleaseTime"].AsDateTime(),
                UploadedByUserId = movieDocument["UploadedByUserId"],
                Rating = movieDocument["Rating"].AsDouble(),
                S3Url = movieDocument["S3Url"]
            };

            var commentsTable = Table.LoadTable(dynamoClient, _commentsTableName);
            var scanFilter = new ScanFilter();
            scanFilter.AddCondition("MovieId", ScanOperator.Equal, id);

            var search = commentsTable.Scan(scanFilter);

            var comments = new List<Comment>();
            while (!search.IsDone)
            {
                var documents = await search.GetNextSetAsync();
                comments.AddRange(documents.Select(doc => new Comment
                {
                    CommentId = doc["CommentId"],
                    MovieId = doc["MovieId"],
                    UserId = doc["UserId"],
                    Content = doc["Content"],
                    CreatedAt = DateTime.Parse(doc["CreatedAt"].AsString())
                }));
            }

            // Pass the movie and comments to the view
            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                Comments = comments
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult AddMovie()
        {
            return View("Add");
        }

        // POST: /Movies/Add
        [HttpPost]
        public async Task<IActionResult> Add(IFormFile movieFile, string title, string genre, string director, DateTime releaseTime, double rating)
        {
            var s3Client = _helper.InitializeS3Client();
            var dynamoClient = _helper.InitializeDynamoDBClient();
            if (movieFile == null || movieFile.Length == 0)
            {
                ModelState.AddModelError("MovieFile", "Please upload a movie file.");
                return View();
            }

            // Upload to S3
            var s3Key = Guid.NewGuid() + Path.GetExtension(movieFile.FileName); // Generate a unique key
            var uploadRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = movieFile.OpenReadStream(),
                ContentType = movieFile.ContentType
            };

            try
            {
                await s3Client.PutObjectAsync(uploadRequest);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error uploading file to S3: " + ex.Message);
                return View();
            }

            // Store metadata in DynamoDB
            var movie = new Movie
            {
                MovieId = Guid.NewGuid().ToString(), // Generate a new MovieId
                Title = title,
                Genre = genre,
                Director = director,
                ReleaseTime = releaseTime,
                UploadedByUserId = User.Identity.Name, // Get the ID of the logged-in user
                Rating = rating,
                S3Url = s3Key // Construct S3 URL
            };

            var table = Table.LoadTable(dynamoClient, _movieTableName);
            var newMovieDocument = new Document
            {
                ["MovieId"] = movie.MovieId,
                ["Title"] = movie.Title,
                ["Genre"] = movie.Genre,
                ["Director"] = movie.Director,
                ["ReleaseTime"] = movie.ReleaseTime.ToString("o"), // ISO 8601 format
                ["UploadedByUserId"] = movie.UploadedByUserId,
                ["Rating"] = movie.Rating,
                ["S3Url"] = movie.S3Url
            };

            await table.PutItemAsync(newMovieDocument);
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> EditMovie(string id)
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            var document = await table.GetItemAsync(id);
            if (document == null || document["UploadedByUserId"] != User.Identity.Name)
            {
                return Forbid();
            }

            var movie = new Movie
            {
                MovieId = document["MovieId"],
                Title = document["Title"],
                Genre = document["Genre"],
                Director = document["Director"],
                ReleaseTime = DateTime.Parse(document["ReleaseTime"]),
                Rating = document["Rating"].AsDouble(),
                S3Url = document["S3Url"]
            };

            return View(movie);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string movieId, IFormFile movieFile, string title, string genre, string director, DateTime releaseTime, double rating)
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var s3Client = _helper.InitializeS3Client();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            var document = await table.GetItemAsync(movieId);
            if (document == null || document["UploadedByUserId"] != User.Identity.Name)
            {
                return Forbid();
            }

            // Update details in DynamoDB
            document["Title"] = title;
            document["Genre"] = genre;
            document["Director"] = director;
            document["ReleaseTime"] = releaseTime.ToString("o"); // ISO 8601 format
            document["Rating"] = rating;

            // Check if a new file is uploaded
            if (movieFile != null && movieFile.Length > 0)
            {
                // Delete the old file from S3
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = document["S3Url"]
                };
                await s3Client.DeleteObjectAsync(deleteRequest);

                // Upload the new file to S3
                var newS3Key = Guid.NewGuid() + Path.GetExtension(movieFile.FileName);
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = newS3Key,
                    InputStream = movieFile.OpenReadStream(),
                    ContentType = movieFile.ContentType
                };

                await s3Client.PutObjectAsync(uploadRequest);
                document["S3Url"] = newS3Key; // Update S3 URL in DynamoDB
            }

            await table.PutItemAsync(document);
            return RedirectToAction("Index");
        }

        // GET: /Movies/Download/{id}
        public async Task<IActionResult> Download(string id)
        {
            var s3Client = _helper.InitializeS3Client();
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);
            var movie = await table.GetItemAsync(id);

            if (movie == null)
                return NotFound();

            var downloadRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = movie["S3Url"]
            };
            var response = await s3Client.GetObjectAsync(downloadRequest);

            return File(response.ResponseStream, response.Headers.ContentType, movie["Title"] + ".mp4");
        }

        // Action to add a comment
        [HttpPost]
        public async Task<IActionResult> AddComment([FromBody] Comment comment)
        {
            if (string.IsNullOrWhiteSpace(comment.Content))
            {
                return Json(new { success = false, message = "Comment content cannot be empty." });
            }

            // Set properties for the comment
            comment.CommentId = Guid.NewGuid().ToString();
            comment.UserId = User.Identity.Name; // assuming user is logged in and User.Identity.Name is available
            comment.CreatedAt = DateTime.UtcNow;

            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _commentsTableName);

            var document = new Document
            {
                ["CommentId"] = comment.CommentId,
                ["MovieId"] = comment.MovieId,
                ["UserId"] = comment.UserId,
                ["Content"] = comment.Content,
                ["CreatedAt"] = comment.CreatedAt
            };

            try
            {
                await table.PutItemAsync(document);
                return Json(new
                {
                    success = true,
                    commentId = comment.CommentId,
                    userId = comment.UserId,
                    content = comment.Content,
                    createdAt = comment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest("An error occurred: " + ex.Message);
            }
        }

        public async Task<IActionResult> HighRated()
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);
            var filter = new ScanFilter();
            filter.AddCondition("Rating", ScanOperator.GreaterThan, 7);
            var search = table.Scan(filter);

            var movies = new List<Movie>();
            do
            {
                var documentList = await search.GetNextSetAsync();
                foreach (var document in documentList)
                {
                    movies.Add(new Movie
                    {
                        MovieId = document["MovieId"],
                        Title = document["Title"],
                        Genre = document["Genre"],
                        Rating = (double)document["Rating"],
                        UploadedByUserId = document["UploadedByUserId"]
                    });
                }
            } while (!search.IsDone);

            return View("HighRatedMovies", movies);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMovie([FromBody] DeleteMovieRequest deleteRequest)
        {
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            // Fetch the movie by its ID
            var document = await table.GetItemAsync(deleteRequest.MovieId);
            if (document == null || document["UploadedByUserId"] != User.Identity.Name)
            {
                return Json(new { success = false, message = "Unauthorized or movie not found." });
            }

            try
            {
                await table.DeleteItemAsync(deleteRequest.MovieId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        public async Task<IActionResult> MoviesByGenre(string genre)
        {
            // Initialize the DynamoDB client and table
            var dynamoClient = _helper.InitializeDynamoDBClient();
            var table = Table.LoadTable(dynamoClient, _movieTableName);

            // If no genre is provided, fetch all movies from the table
            var filter = new ScanFilter();
            if (!string.IsNullOrEmpty(genre))
            {
                filter.AddCondition("Genre", ScanOperator.Equal, genre); // Filter by selected genre
            }

            // Execute the scan with the filter
            var search = table.Scan(filter);

            var movies = new List<Movie>();
            do
            {
                var documentList = await search.GetNextSetAsync();
                foreach (var document in documentList)
                {
                    movies.Add(new Movie
                    {
                        MovieId = document["MovieId"],
                        Title = document["Title"],
                        Genre = document["Genre"],
                        Rating = (double)document["Rating"],
                        UploadedByUserId = document["UploadedByUserId"],
                        ReleaseTime = (DateTime)document["ReleaseTime"]
                    });
                }
            } while (!search.IsDone);

            // Return the view with the list of movies
            return View("MoviesByGenre", movies); // This will render the view with movies based on the genre
        }


        [HttpPost]
        public async Task<IActionResult> EditComment([FromBody] Comment comment)
        {
            if (string.IsNullOrWhiteSpace(comment.Content) || string.IsNullOrWhiteSpace(comment.CommentId))
            {
                return Json(new { success = false, message = "Content cannot be empty." });
            }

            var dynamoClient = _helper.InitializeDynamoDBClient();
            var commentsTable = Table.LoadTable(dynamoClient, _commentsTableName);

            // Fetch the comment from DynamoDB
            var document = await commentsTable.GetItemAsync(comment.CommentId);
            if (document == null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            // Ensure the current user is the one who created the comment
            if (document["UserId"] != User.Identity.Name)
            {
                return Forbid();
            }

            // Update the content of the comment
            document["Content"] = comment.Content;
            document["CreatedAt"] = DateTime.UtcNow.ToString("o"); // Update the timestamp to current time

            try
            {
                // Save the updated comment back to DynamoDB
                await commentsTable.PutItemAsync(document);
                return Json(new
                {
                    success = true,
                    commentId = document["CommentId"],
                    userId = document["UserId"],
                    content = document["Content"],
                    createdAt = document["CreatedAt"]
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

    }
}
