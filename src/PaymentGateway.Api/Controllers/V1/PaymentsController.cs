namespace PaymentGateway.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PaymentsController(IPaymentService paymentService) : Controller
{
    /// <summary>
    /// Process a payment through the payment gateway
    /// </summary>
    /// <param name="request">Payment request containing card details, amount, and currency</param>
    /// <param name="idempotencyKey">A unique identifier for this payment request to prevent duplicate processing. Must be provided in the Idempotency-Key header. The same key cannot be reused for different payment requests.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Payment response with status and transaction details</returns>
    /// <response code="200">Payment was successfully processed (Authorized or Declined)</response>
    /// <response code="400">Payment was rejected due to invalid information or validation errors</response>
    /// <response code="500">Internal server error occurred during payment processing</response>
    /// <remarks>
    /// A merchant can process a payment and receive one of the following response types:
    /// 
    /// - **Authorized**: The payment was authorized by the call to the acquiring bank
    /// - **Declined**: The payment was declined by the call to the acquiring bank  
    /// - **Rejected**: No payment could be created as invalid information was supplied to the payment gateway and therefore it has rejected the request without calling the acquiring bank
    /// 
    /// **Idempotency**: This endpoint requires an `Idempotency-Key` header to prevent duplicate payments. 
    /// Each key can only be used once. If the same key is provided multiple times, subsequent requests 
    /// will be rejected to ensure payment safety.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/payments
    ///     Headers:
    ///         Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
    ///         Content-Type: application/json
    ///     Body:
    ///     {
    ///         "cardNumber": "2222405343248877",
    ///         "expiryMonth": 9,
    ///         "expiryYear": 2025,
    ///         "currency": "GBP",
    ///         "amount": 100,
    ///         "cvv": "123"
    ///     }
    /// 
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync([FromBody] PostPaymentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        (PostPaymentResponse response, List<string> errors) = await paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        if (errors.Count != 0 || response.Status == PaymentStatus.Rejected)
        {
            return Problem(
                title: "Payment rejected",
                detail: string.Join("; ", errors),
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"
            );
        }

        return Ok(response);
    }

    /// <summary>
    /// Retrieve details of a previously made payment using its identifier
    /// </summary>
    /// <param name="id">The unique identifier of the payment to retrieve</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Payment details including masked card information and payment status</returns>
    /// <response code="200">Payment details were successfully retrieved</response>
    /// <response code="404">Payment with the specified identifier was not found</response>
    /// <remarks>
    /// This endpoint allows a merchant to retrieve details of a previously made payment using its unique identifier.
    /// This functionality helps merchants with their reconciliation and reporting needs.
    /// 
    /// The response includes:
    /// - **Masked card number**: Only the last four digits of the card number are returned for security
    /// - **Card details**: Expiry month and year of the card used
    /// - **Payment status**: Indicates the result of the payment (Authorized, Declined, or Rejected)
    /// - **Transaction details**: Amount and currency of the payment
    /// 
    /// Sample request:
    /// 
    ///     GET /api/v1/payments/12345678-1234-1234-1234-123456789012
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "id": "12345678-1234-1234-1234-123456789012",
    ///         "status": "Authorized",
    ///         "cardNumberLastFour": 8877,
    ///         "expiryMonth": 4,
    ///         "expiryYear": 2025,
    ///         "currency": "GBP",
    ///         "amount": 100
    ///     }
    /// 
    /// </remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GetPaymentResponse> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        GetPaymentResponse? payment = paymentService.GetPayment(id, cancellationToken);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}