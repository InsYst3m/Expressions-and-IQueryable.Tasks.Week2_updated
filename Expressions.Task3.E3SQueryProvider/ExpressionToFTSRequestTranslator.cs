using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                ConstantExpression expression = null;

                switch (node.Method.Name)
                {
                    case "StartsWith":
                        expression = Expression.Constant($"{(node.Arguments[0] as ConstantExpression)?.Value}*");
                        break;

                    case "EndsWith":
                        expression = Expression.Constant($"*{(node.Arguments[0] as ConstantExpression)?.Value}");
                        break;

                    case "Contains":
                        expression = Expression.Constant($"*{(node.Arguments[0] as ConstantExpression)?.Value}*");
                        break;

                    default:
                        expression = Expression.Constant($"{(node.Arguments[0] as ConstantExpression)?.Value}");
                        break;
                }

                var beforeMethodExpression = node.Object as MemberExpression;
                if (beforeMethodExpression == null)
                    throw new InvalidOperationException($"expression before method {node.Method.Name} is null.");

                var comparisonExpression = Expression.Equal(beforeMethodExpression, expression);

                var result = Visit(comparisonExpression) ?? throw new InvalidOperationException("Result expression is null.");

                return result;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Expression left = node.Left;
            Expression right = node.Right;

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    if (node.Left.NodeType != ExpressionType.MemberAccess)
                    {
                        left = node.Right;
                        right = node.Left;
                    }

                    if (left != null && left.NodeType != ExpressionType.MemberAccess)
                        throw new NotSupportedException(string.Format("Left operand should be property or field", node.NodeType));

                    if (right != null && right.NodeType != ExpressionType.Constant)
                        throw new NotSupportedException(string.Format("Right operand should be constant", node.NodeType));

                    Visit(left);
                    _resultStringBuilder.Append("(");
                    Visit(right);
                    _resultStringBuilder.Append(")");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
