using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace NotifyPropertyChangedIssue
{
    [ExportSyntaxNodeCodeIssueProvider("NotifyPropertyChangedIssue", LanguageNames.CSharp, typeof(LiteralExpressionSyntax))]
    class NotifyPropertyChangedIssueProvider : ICodeIssueProvider
    {
        private readonly ICodeActionEditFactory editFactory;

        [ImportingConstructor]
        public NotifyPropertyChangedIssueProvider(ICodeActionEditFactory editFactory)
        {
            this.editFactory = editFactory;
        }

        private T FindParent<T>(CommonSyntaxNode startNode) where T: CommonSyntaxNode
        {
            CommonSyntaxNode currentNode = startNode;
            while (currentNode != null &&
                ((currentNode is T) == false))
            {
                currentNode = currentNode.Parent;
            }

            return (T)currentNode;
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxNode node, CancellationToken cancellationToken)
        {
            CommonSyntaxTree tree = document.GetSyntaxTree();
            ISemanticModel model = document.GetSemanticModel();

            List<CommonSyntaxToken> badTokens = new List<CommonSyntaxToken>();
            try
            {
                LiteralExpressionSyntax literalExpression = (LiteralExpressionSyntax)node;
                
                InvocationExpressionSyntax invocation = FindParent<InvocationExpressionSyntax>(literalExpression);

                if (invocation != null)
                {
                    string invocationIdentifier = invocation.DescendentNodes().OfType<IdentifierNameSyntax>().First().PlainName;

                    if (invocationIdentifier == "NotifyPropertyChanged")
                    {
                        PropertyDeclarationSyntax property = FindParent<PropertyDeclarationSyntax>(invocation);

                        if (property != null)
                        {
                            ISymbol propertySymbol = model.GetDeclaredSymbol(property);

                            if (propertySymbol.Name != literalExpression.Token.ValueText)
                            {
                                badTokens.Add(literalExpression.Token);
                            }
                        }
                    }
                }
            }
            catch { }

            foreach (CommonSyntaxToken token in badTokens)
            {
                yield return new CodeIssue(CodeIssue.Severity.Warning, token.Span, "Parameter name does not match property name.");
            }
        }

        #region Unimplemented ICodeIssueProvider members

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxToken token, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CodeIssue> GetIssues(IDocument document, CommonSyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
