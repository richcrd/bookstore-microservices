namespace Catalog.Application.DTOs;

public record UpdateBookPriceRequest(decimal NewPrice, string Currency);