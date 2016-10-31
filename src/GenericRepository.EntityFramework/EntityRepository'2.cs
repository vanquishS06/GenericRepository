////////////////////////////////////////////////////////////////////////////////
///
/// Generic Repository based on:
/// http://www.tugberkugurlu.com/archive/clean-better-and-sexier-generic-repository-implementation-for-entity-framework
/// 
/// Allows sharing of dbContext instance between repositories
/// Provides generic repository interfaces
/// Pagination support.
/// Allows pass a fake DbContext instance into the repository implementation itself
///
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Data.Entity.Validation;

namespace GenericRepository.EntityFramework
{  
    /// <summary>
    /// IEntityRepository implementation for DbContext instance.
    /// </summary>
    /// <typeparam name="TEntity">Type of entity</typeparam>
    /// <typeparam name="TId">Type of entity Id</typeparam>
    public class EntityRepository<TEntity, TId> : IEntityRepository<TEntity, TId> 
        where TEntity : class, IEntity<TId>
        where TId : IComparable 
    {
        private readonly IEntitiesContext _dbContext;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="dbContext"></param>
        public EntityRepository(IEntitiesContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException("dbContext");

            _dbContext = dbContext;
        }

        #region Create Records
        /// <summary>
        /// Add in Single Record in the dataSet
        /// </summary>
        /// <param name="entity">Generics dbSet type</param>
        public TEntity Add(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new NullReferenceException("Attempt to add a null record");

                IDbSet<TEntity> dbSet = _dbContext.Set<TEntity>();
                TEntity addedEntity = dbSet.Add(entity);
                _dbContext.SetAsCreated(entity);
                return addedEntity;
            }
            catch (DbEntityValidationException dbEx)
            {
                throwException(dbEx);
                return null;
            }
        }

        public void AddGraph(TEntity entity)
        {
            _dbContext.Set<TEntity>().Add(entity);
        }
        #endregion Create Records

        #region Read Records
        public IQueryable<TEntity> GetAll()
        {
            IQueryable<TEntity> dbSet = _dbContext.Set<TEntity>();
            return dbSet;
        }

        public IQueryable<TEntity> GetAllIncluding(params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> queryable = GetAll();
            foreach (Expression<Func<TEntity, object>> includeProperty in includeProperties)
                queryable = queryable.Include<TEntity, object>(includeProperty);

            return queryable;
        }

        public TEntity GetSingle(TId id)
        {
            IQueryable<TEntity> entities = GetAll();
            TEntity entity = Filter<TId>(entities, x => x.Id, id).FirstOrDefault();
            return entity;
        }

        public TEntity GetSingleIncluding(TId id, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> entities = GetAllIncluding(includeProperties);
            TEntity entity = Filter<TId>(entities, x => x.Id, id).FirstOrDefault();
            return entity;
        }

        public IQueryable<TEntity> FindBy(Expression<Func<TEntity, bool>> predicate)
        {
            IQueryable<TEntity> queryable = GetAll().Where<TEntity>(predicate);
            return queryable;
        }
        #endregion Read a record

        #region Update Records
        public TEntity Update(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");
                _dbContext.SetAsUpdated(entity);
                return entity;
            }
            catch (DbEntityValidationException dbEx)
            {
                throwException(dbEx);
                return null;
            }
        }
        #endregion Update Records

        #region Delete Records
        public void Delete(TEntity entity) 
        {
            _dbContext.SetAsDeleted(entity);
        }
        #endregion Delete Records

        #region Pagination
        public PaginatedList<TEntity> Paginate(int pageIndex, int pageSize)
        {
            PaginatedList<TEntity> paginatedList = Paginate<TId>(pageIndex, pageSize, x => x.Id);
            return paginatedList;
        }

        public PaginatedList<TEntity> Paginate<TKey>(int pageIndex, int pageSize, Expression<Func<TEntity, TKey>> keySelector)
        {
            return Paginate<TKey>(pageIndex, pageSize, keySelector, null);
        }

        public PaginatedList<TEntity> Paginate<TKey>(int pageIndex, int pageSize, Expression<Func<TEntity, TKey>> keySelector, Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties)
        {

            PaginatedList<TEntity> paginatedList = Paginate<TKey>(
                pageIndex, pageSize, keySelector, predicate, OrderByType.Ascending, includeProperties);

            return paginatedList;
        }

        public PaginatedList<TEntity> PaginateDescending<TKey>(int pageIndex, int pageSize, Expression<Func<TEntity, TKey>> keySelector)
        {
            return PaginateDescending<TKey>(pageIndex, pageSize, keySelector, null);
        }

        public PaginatedList<TEntity> PaginateDescending<TKey>(int pageIndex, int pageSize, Expression<Func<TEntity, TKey>> keySelector, Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            PaginatedList<TEntity> paginatedList = Paginate<TKey>(
                pageIndex, pageSize, keySelector, predicate, OrderByType.Descending, includeProperties);

            return paginatedList;
        }
        #endregion Pagination

        public int GetCount()
        {
            IQueryable<TEntity> dbSet = GetAll();
            int count = dbSet.ToList().Count();
            return count;
        }

        public virtual void Detach(TEntity entity)
        {
            _dbContext.SetAsDetached<TEntity>(entity);
        }

        public int Save()
        {
            return _dbContext.SaveChanges();
        }

        #region Helpers
        private PaginatedList<TEntity> Paginate<TKey>(int pageIndex, int pageSize, Expression<Func<TEntity, TKey>> keySelector, Expression<Func<TEntity, bool>> predicate, OrderByType orderByType, params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> queryable =
                (orderByType == OrderByType.Ascending)
                    ? GetAllIncluding(includeProperties).OrderBy(keySelector)
                    : GetAllIncluding(includeProperties).OrderByDescending(keySelector);

            queryable = (predicate != null) ? queryable.Where(predicate) : queryable;
            PaginatedList<TEntity> paginatedList = queryable.ToPaginatedList(pageIndex, pageSize);

            return paginatedList;
        }

        private IQueryable<TEntity> Filter<TProperty>(IQueryable<TEntity> dbSet, 
            Expression<Func<TEntity, TProperty>> property, TProperty value)
            where TProperty : IComparable
        {
            var memberExpression = property.Body as MemberExpression;
            if (memberExpression == null || !(memberExpression.Member is PropertyInfo))
                throw new ArgumentException("Property expected", "property");

            Expression left = property.Body;
            Expression right = Expression.Constant(value, typeof(TProperty));
            Expression searchExpression = Expression.Equal(left, right);
            Expression<Func<TEntity, bool>> lambda = Expression.Lambda<Func<TEntity, bool>>(
                searchExpression, new ParameterExpression[] { property.Parameters.Single() });

            return dbSet.Where(lambda);
        }

        /// <summary>
        /// Throw a db Validation exception
        /// </summary>
        /// <param name="dbEx">exception</param>
        private void throwException(DbEntityValidationException dbEx)
        {
            var msg = string.Empty;

            foreach (var validationErrors in dbEx.EntityValidationErrors)
            {
                foreach (var validationError in validationErrors.ValidationErrors)
                    msg += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
            }
            var ex = new Exception(msg, dbEx);
            throw ex;
        }

        private enum OrderByType 
        {
            Ascending,
            Descending
        }
        #endregion Helpers
    }
}