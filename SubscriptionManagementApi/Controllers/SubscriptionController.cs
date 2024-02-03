using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;
using SubscriptionManagementApi.Data;
using SubscriptionManagementApi.Models;
using SubscriptionManagementApi.Repositories;
using SubscriptionManagementApi.Services;
using System.Security.Claims;

namespace SubscriptionManagementApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        #region Private Prop
        private readonly ApplicationDbContext _context;
        private readonly SubscriptionRepository _subscriptionRepository;     
        private readonly ILogger<SubscriptionController> _logger;
        #endregion

        #region CTOR
        public SubscriptionController(ApplicationDbContext context, SubscriptionRepository subscriptionRepository, AuthenticationService authenticationService, ILogger<SubscriptionController> logger)
        {
            _context = context;
            _subscriptionRepository = subscriptionRepository;
            _logger = logger;
        }
        #endregion

        #region Public Methods (Api Methods)
        [HttpGet("getSubscriptionsByUser")]
        public IActionResult GetSubscriptionsByUserId([FromQuery] int userId)
        {
            try
            {
                _logger.LogInformation($"Getting subscriptions for userId: {userId}");

                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId provided for getting subscriptions.");
                    return BadRequest("Invalid userId provided.");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var subscriptions = _subscriptionRepository.GetSubscriptionsByUserId(userId);
                    return Task.FromResult(subscriptions.ToList());
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation($"Successfully retrieved subscriptions for userId: {userId}");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error retrieving subscriptions for userId {userId}. {result.Result.FinalException}");
                    return StatusCode(500, "Error retrieving subscriptions. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while getting subscriptions for userId {userId}: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("getActiveSubscriptions")]
        public IActionResult GetActiveSubscriptions()
        {
            try
            {
                _logger.LogInformation("Getting active subscriptions");

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var subscriptions = _subscriptionRepository.GetActiveSubscriptions();
                    return Task.FromResult(subscriptions.ToList());
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation("Successfully retrieved active subscriptions");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error retrieving active subscriptions. {result.Result.FinalException}");
                    return StatusCode(500, "Error retrieving active subscriptions. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while getting active subscriptions: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("calculateRemainingDays")]
        public IActionResult CalculateRemainingDays([FromQuery] int subscriptionId)
        {
            try
            {
                _logger.LogInformation($"Calculating remaining days for subscriptionId: {subscriptionId}");

                if (subscriptionId <= 0)
                {
                    _logger.LogError("Invalid subscriptionId provided for calculating remaining days.");
                    return BadRequest("Invalid subscriptionId provided.");
                }

                // check if provided subscritption is active or not //
                var activeSubscriptions = _context.GetActiveSubscriptions();
                if(activeSubscriptions != null && activeSubscriptions.Any() 
                    && !activeSubscriptions.Any(x => x.SubscriptionId == subscriptionId))
                {
                    _logger.LogError("Subscription provided not active yet.");
                    return BadRequest("Subscription not active");
                }

                var subscription = _context.Subscriptions.Find(subscriptionId);

                if (subscription is null)
                {
                    _logger.LogError("Subscription provided for calculating remaining days not found.");
                    return BadRequest("Subscription not found");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var remainingDays = _subscriptionRepository.CalculateRemainingDays(subscriptionId);
                    return Task.FromResult(remainingDays);
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation($"Successfully calculated remaining days for subscriptionId: {subscriptionId}");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error calculating remaining days for subscriptionId {subscriptionId}. {result.Result.FinalException}");
                    return StatusCode(500, "Error calculating remaining days. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while calculating remaining days for subscriptionId {subscriptionId}: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }      

        [HttpPost("create")]
        public IActionResult CreateSubscription([FromBody] Subscription subscription)
        {
            try
            {
                _logger.LogInformation("Creating subscription");

                if (subscription == null)
                {
                    _logger.LogError("Invalid subscription data provided for creating subscription.");
                    return BadRequest("Invalid subscription data provided.");
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId provided for creating subscription.");
                    return BadRequest("Invalid userId provided.");
                }

                subscription.UserId = userId;

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    _context.Subscriptions.Add(subscription);
                    _context.SaveChanges();

                    return Task.FromResult<IActionResult>(Ok("Subscription created successfully"));
                    
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation("Successfully created subscription");
                    return Ok(result.Result.Result);  
                }
                else
                {
                    _logger.LogError($"Error creating subscription. {result.Result.FinalException}");
                    return StatusCode(500, "Error creating subscription. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while creating subscription: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("update")]
        public IActionResult UpdateSubscription([FromQuery] int subscriptionId, [FromBody] Subscription updatedSubscription)
        {
            try
            {
                _logger.LogInformation($"Updating subscription: {subscriptionId}");

                if (subscriptionId <= 0 || updatedSubscription == null)
                {
                    _logger.LogError("Invalid subscriptionId or subscription data provided for updating subscription.");
                    return BadRequest("Invalid subscriptionId or subscription data provided.");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var existingSubscription = _context.Subscriptions.Find(subscriptionId);

                    if (existingSubscription == null)
                    {
                        return Task.FromResult<IActionResult>(NotFound("Subscription not found"));
                    }

                    // Update subscription properties 
                    existingSubscription.UserId = updatedSubscription.UserId;
                    existingSubscription.StartDate = updatedSubscription.StartDate;
                    existingSubscription.EndDate = updatedSubscription.EndDate;
                    existingSubscription.SubscriptionType = updatedSubscription.SubscriptionType;

                    _context.SaveChanges();

                    return Task.FromResult<IActionResult>(Ok("Subscription updated successfully"));
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation($"Successfully updated subscription: {subscriptionId}");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error updating subscription {subscriptionId}. {result.Result.FinalException}");
                    return StatusCode(500, "Error updating subscription. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while updating subscription {subscriptionId}: {ex}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("delete")]
        public IActionResult DeleteSubscription([FromQuery] int subscriptionId)
        {
            try
            {
                _logger.LogInformation($"Deleting subscription: {subscriptionId}");

                if (subscriptionId <= 0)
                {
                    _logger.LogError("Invalid subscriptionId provided for deleting subscription.");
                    return BadRequest("Invalid subscriptionId provided.");
                }

                var result = RetryPolicy().ExecuteAndCaptureAsync(() =>
                {
                    var subscriptionToDelete = _context.Subscriptions.Find(subscriptionId);

                    if (subscriptionToDelete == null)
                    {
                        return Task.FromResult<IActionResult>(NotFound("Subscription not found"));
                    }

                    _context.Subscriptions.Remove(subscriptionToDelete);
                    _context.SaveChanges();

                    return Task.FromResult<IActionResult>(Ok("Subscription deleted successfully"));
                });

                if (result.Result.Outcome == OutcomeType.Successful)
                {
                    _logger.LogInformation($"Successfully deleted subscription: {subscriptionId}");
                    return Ok(result.Result.Result);
                }
                else
                {
                    _logger.LogError($"Error deleting subscription {subscriptionId}. {result.Result.FinalException}");
                    return StatusCode(500, "Error deleting subscription. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred while deleting subscription {subscriptionId}: {ex}");
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
