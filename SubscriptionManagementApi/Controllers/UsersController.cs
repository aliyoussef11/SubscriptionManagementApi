using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;
using SubscriptionManagementApi.Data;
using SubscriptionManagementApi.Models;
using SubscriptionManagementApi.Services;

namespace SubscriptionManagementApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        #region Private Prop
        private readonly ILogger<SubscriptionController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly AuthenticationService _authenticationService;
        #endregion

        #region CTOR
        public UsersController(ApplicationDbContext context, AuthenticationService authenticationService, ILogger<SubscriptionController> logger)
        {
            _context = context;
            _authenticationService = authenticationService;
            _logger = logger;
        }
        #endregion

        #region Public Methods (Api Methods)
        [HttpPost("create")]
        public IActionResult CreateUser([FromBody] User user)
        {
            try
            {
                _logger.LogInformation("Creating user");

                if (user == null)
                {
                    _logger.LogError("Invalid user data provided for creating user.");
                    return BadRequest("Invalid user data provided.");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    _context.Users.Add(user);
                    _context.SaveChanges();

                    return Task.FromResult<IActionResult>(Ok("User created successfully"));

                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation("Successfully created user");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error creating user. {result.Result.FinalException}");
                    return StatusCode(500, "Error creating user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while creating user: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost("authenticate")]
        [AllowAnonymous]
        public IActionResult Authenticate([FromBody] LoginModel model)
        {
            try
            {
                _logger.LogInformation($"Authenticating user: {model?.Username}");

                if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                {
                    _logger.LogError("Invalid credentials provided for authentication.");
                    return BadRequest("Invalid credentials provided.");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var user = _context.Users.SingleOrDefault(u => u.Username == model.Username && u.PasswordHash == model.Password);

                    if (user == null)
                    {
                        return Task.FromResult<IActionResult>(BadRequest("Invalid credentials"));
                    }

                    var token = _authenticationService.GenerateJwtToken(user);

                    return Task.FromResult<IActionResult>(Ok(new { Token = token }));
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation($"Successfully authenticated user: {model.Username}");
                    return result.Result.Result;
                }
                else
                {
                    _logger.LogError($"Error authenticating user {model.Username}. {result.Result.FinalException}");
                    return StatusCode(500, "Error authenticating user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while authenticating user {model?.Username}: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
        #endregion

        #region Private Functions
        private static IAsyncPolicy RetryPolicy()
        {
            return Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
        #endregion
    }
}
