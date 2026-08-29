namespace Catalog.Domain.Exceptions;

public class CatalogNotFoundException(string message) : CatalogDomainException(message);