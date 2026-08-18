namespace Domain.Enums;

/// <summary>
/// Define os tipos de recursos e capacidades de IA disponíveis no microsserviço.
/// </summary>
public enum AiFeatureType
{
    /// <summary>
    /// Gerador de descrição comercial e técnica de produtos.
    /// </summary>
    ProductDescription = 1,

    /// <summary>
    /// Sugestão de tags e palavras-chave de busca para catálogo.
    /// </summary>
    ProductTags = 2,

    /// <summary>
    /// Sugestão de categoria ideal para um produto.
    /// </summary>
    CategorySuggestion = 3,

    /// <summary>
    /// Resumo analítico de fatura/nota fiscal.
    /// </summary>
    InvoiceSummary = 4
}
