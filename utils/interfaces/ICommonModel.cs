

namespace project.utils.interfaces
{
    public interface ICommonModel<IdClass> : ICommonModelHeader
    {
        public IdClass Id { get; set; }

    }
}