using System;
using System.Data.Entity;

namespace GenericRepository.EntityFramework
{
    public interface IEntitiesContext : IDisposable
    {
        IDbSet<TEntity> Set<TEntity>() where TEntity : class;

        void SetAsCreated<TEntity>(TEntity entity) where TEntity : class;
        void SetAsUpdated<TEntity>(TEntity entity) where TEntity : class;
        void SetAsDeleted<TEntity>(TEntity entity) where TEntity : class;

        int SaveChanges();
    }
}