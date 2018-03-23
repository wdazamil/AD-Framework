using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AD.Core.Repository
{
    public interface IBaseRepo<T>
    {
        T GetById(int id);
        ICollection<T> GetAll();
        ICollection<T> GetByFilter(Func<T, bool> predicate);
        T Add(T obj);
        bool Delete(T obj);
        T Update(T obj);
        Task<T> GetByIdAsync(int id);
        Task<ConcurrentBag<T>> GetAllAsync();
        Task<ConcurrentBag<T>> GetByFilterAsync(Func<T, bool> predicate);
        Task<T> AddAsync(T obj);
        Task<bool> DeleteAsync(T obj);
        Task<T> UpdateAsync(T obj);
    }
}
