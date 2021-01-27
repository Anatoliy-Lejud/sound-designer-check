using Assets.UBindr.Expressions;

namespace Assets.UBindr
{
    public interface IBindingSource
    {
        void Register(TopDownParser.Scope scope, bool global, object source);
    }
}