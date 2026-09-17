namespace SSO.Application.Helper
{
    public sealed class TokenValidator
    {
        public IReadOnlyList<string> Validate<TModel>(IReadOnlySet<string> tokens, TModel model)
        {
            var props = typeof(TModel)
                .GetProperties()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return tokens.Where(t => !props.Contains(t)).ToList();
        }
    }
}
