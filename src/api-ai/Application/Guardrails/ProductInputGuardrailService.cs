using System.Text.RegularExpressions;

namespace Application.Guardrails;

public sealed partial class ProductInputGuardrailService : IGuardrailService
{
    private const int MaxProductNameLength = 150;
    private const int MaxDescriptionHintLength = 500;

    // Padrões conhecidos de Prompt Injection, Jailbreak e Escape de Instruções
    [GeneratedRegex(@"(?i)\b(ignore|desconsidere|esque[cç]a|bypass|disregard|override|substitua|reset)\b.{0,40}\b(all|previous|above|the|as|suas|anteriores|instru[cç][oõ]es|instructions|rules|regras|prompts|diretrizes|safety|guidelines)\b", RegexOptions.Compiled)]
    private static partial Regex InjectionPromptOverrideRegex();

    [GeneratedRegex(@"(?i)\b(you\s+are\s+now|voc[eê]\s+agora\s+[eé]|aja\s+como|act\s+as\s+a|pretend\s+to\s+be|finja\s+que\s+[eé]|roleplay\s+as|modo\s+dan|dan\s+mode|jailbreak|developer\s+mode)\b", RegexOptions.Compiled)]
    private static partial Regex RoleplayJailbreakRegex();

    [GeneratedRegex(@"(?i)\b(system\s+prompt|prompt\s+do\s+sistema|system\s+message|system:\s*|assistant:\s*|user:\s*|<system>|</system>|<prompt>|</prompt>)\b", RegexOptions.Compiled)]
    private static partial Regex SystemDelimiterLeakRegex();

    // Padrões de uso conversacional indevido como Chatbot
    [GeneratedRegex(@"(?i)^\s*(ol[aá]|oi|bom\s+dia|boa\s+tarde|boa\s+noite|hello|hi|hey|opa)\b(\s*[,!?.]|\s+(como\s+vai|tudo\s+bem|voc[eê]\s+pode|me\s+ajude|quem\s+[eé]))", RegexOptions.Compiled)]
    private static partial Regex ConversationalGreetingRegex();

    [GeneratedRegex(@"(?i)\b(quem\s+[eé]\s+voc[eê]|who\s+are\s+you|o\s+que\s+voc[eê]\s+[eé]|qual\s+[eé]\s+a\s+sua\s+fun[cç][aã]o|me\s+conte\s+uma\s+hist[oó]ria|escreva\s+um\s+poema|conte\s+uma\s+piada|me\s+d[eê]\s+uma\s+receita|qual\s+[eé]\s+a\s+capital)\b", RegexOptions.Compiled)]
    private static partial Regex ConversationalQuestionsRegex();

    [GeneratedRegex(@"(?i)\b(escreva\s+um\s+c[oó]digo|write\s+(a\s+)?code|crie\s+um\s+script|create\s+(a\s+)?script|gere\s+um\s+sql|select\s+\*\s+from|drop\s+table)\b", RegexOptions.Compiled)]
    private static partial Regex CodeGenerationMisuseRegex();

    // Limpeza de Saída (Remoção de prefixos conversacionais de LLM)
    [GeneratedRegex(@"(?i)^(aqui\s+est[aá]\s+(a\s+)?descri[cç][aã]o(\s+do\s+produto|\s+solicitada|\s+do\s+item)?|com\s+certeza!?|claro!?|certamente!?|como\s+(uma\s+)?intelig[eê]ncia\s+artificial|como\s+modelo\s+de\s+linguagem|ol[aá]!?:?|segue\s+a\s+descri[cç][aã]o:?)\s*[:,\-]?\s*", RegexOptions.Compiled)]
    private static partial Regex OutputChatPrefixRegex();

    public GuardrailValidationResult ValidateProductInput(string productName, string? descriptionHint = null)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return GuardrailValidationResult.Blocked("O nome do produto é obrigatório.");
        }

        var trimmedName = productName.Trim();

        // 1. Validação de Tamanho Excessivo
        if (trimmedName.Length > MaxProductNameLength)
        {
            return GuardrailValidationResult.Blocked(
                $"O nome do produto excede o tamanho máximo permitido de {MaxProductNameLength} caracteres. Digite apenas o nome/modelo do item.");
        }

        if (descriptionHint?.Length > MaxDescriptionHintLength)
        {
            return GuardrailValidationResult.Blocked(
                $"As informações adicionais excedem o limite de {MaxDescriptionHintLength} caracteres.");
        }

        // 2. Validação contra Múltiplas Quebras de Linha no Nome
        if (trimmedName.Contains('\n') || trimmedName.Contains('\r'))
        {
            return GuardrailValidationResult.Blocked(
                "O nome do produto não pode conter quebras de linha.");
        }

        // 3. Detecção de Prompt Injections e Quebra de Regras
        if (InjectionPromptOverrideRegex().IsMatch(trimmedName) || 
            (descriptionHint != null && InjectionPromptOverrideRegex().IsMatch(descriptionHint)))
        {
            return GuardrailValidationResult.Blocked(
                "Entrada inválida detectada (tentativa de alteração de instruções do sistema ou injeção de prompt).");
        }

        if (RoleplayJailbreakRegex().IsMatch(trimmedName) || 
            (descriptionHint != null && RoleplayJailbreakRegex().IsMatch(descriptionHint)))
        {
            return GuardrailValidationResult.Blocked(
                "Entrada inválida detectada (tentativa de jailbreak ou personificação não autorizada).");
        }

        if (SystemDelimiterLeakRegex().IsMatch(trimmedName) || 
            (descriptionHint != null && SystemDelimiterLeakRegex().IsMatch(descriptionHint)))
        {
            return GuardrailValidationResult.Blocked(
                "Entrada contém marcadores de sistema ou delimitadores reservados.");
        }

        // 4. Detecção de Uso Conversacional / Chatbot
        if (ConversationalGreetingRegex().IsMatch(trimmedName) || ConversationalQuestionsRegex().IsMatch(trimmedName))
        {
            return GuardrailValidationResult.Blocked(
                "Este serviço destina-se exclusivamente à geração de descrições de catálogo. Mensagens conversacionais ou perguntas de chat não são permitidas.");
        }

        if (CodeGenerationMisuseRegex().IsMatch(trimmedName))
        {
            return GuardrailValidationResult.Blocked(
                "Solicitações de código ou comandos executáveis não são permitidos como nome de produto.");
        }

        // 5. Sanitização
        var sanitizedName = SanitizeText(trimmedName);
        var sanitizedHint = string.IsNullOrWhiteSpace(descriptionHint) ? null : SanitizeText(descriptionHint.Trim());

        return GuardrailValidationResult.Success(sanitizedName, sanitizedHint);
    }

    public string CleanAndValidateOutput(string generatedText)
    {
        if (string.IsNullOrWhiteSpace(generatedText)) return string.Empty;

        var cleaned = generatedText.Trim();

        // Remove aspas envolventes se a IA tiver colocado
        if (cleaned.StartsWith('"') && cleaned.EndsWith('"') && cleaned.Length > 2)
        {
            cleaned = cleaned[1..^1].Trim();
        }

        // Remove prefixos conversacionais como "Aqui está a descrição do produto:", "Claro!...", etc.
        cleaned = OutputChatPrefixRegex().Replace(cleaned, string.Empty).Trim();

        return cleaned;
    }

    private static string SanitizeText(string input)
    {
        // Remove tags HTML/XML ou caracteres delimitadores que possam tentar fechar o bloco de prompt
        var sanitized = input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("`", "'");

        // Normaliza espaços duplicados
        return Regex.Replace(sanitized, @"\s+", " ").Trim();
    }
}
