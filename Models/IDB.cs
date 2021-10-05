using System.Collections.Generic;

namespace Connect.DB
{
    public interface IDB<T>
    {
        void Delete(int id);
        IDictionary<int, T> Read();
        T Read(int id);
        void Write(IDictionary<int, T> values, bool overwrite = true);
        void Write(int id, T value, bool overwrite = true);
    }
}