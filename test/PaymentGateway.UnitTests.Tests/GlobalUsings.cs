global using System.Net;
global using System.Text.Json;

global using PaymentGateway.Domain.Enums;
global using PaymentGateway.Domain.Models.Requests;
global using PaymentGateway.Application.Interfaces;
global using PaymentGateway.Domain.Models.Responses;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Testing;

global using NSubstitute;
global using NSubstitute.ExceptionExtensions;