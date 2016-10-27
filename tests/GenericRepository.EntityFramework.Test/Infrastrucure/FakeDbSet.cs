﻿///////////////////////////////////////////////////////////////////////////
///
/// Class:   FakeDbSet<TEntity>: Generic IDbSet instantiation
///
/// Use:     var myTestDbSet = new FakeDbSet<myTestDbClass>();
/// 
/// Description: Create a Database fake instance using an ObservableCollection 
///              for Unit Tests.
///
/// Comments:
/// ObservableCollection: collection that allows code outside the collection 
/// to be aware of when changes to the collection occur by implementing:
///    * INotifyCollectionChanged
///    * INotifyPropertyChanged
///    
/// Handler could be added as follows:
///   public FakeDbSet()
///   {
///      _collection.CollectionChanged += HandleChange;
///      ...
///        
///   private void HandleChange(object sender, NotifyCollectionChangedEventArgs e)
///   {
///      // do something..
///
///////////////////////////////////////////////////////////////////////////

// microsoft .NET
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace GenericRepository.EntityFramework.Test.Infrastrucure 
{   
    public class FakeDbSet<TEntity> : IDbSet<TEntity> where TEntity : class
    {
        ObservableCollection<TEntity> _collection;
        IQueryable _query;

        public FakeDbSet()
        {
            _collection = new ObservableCollection<TEntity>();
            //  Converts an IEnumerable to an IQueryable
            _query = _collection.AsQueryable();
        }

        public TEntity Add(TEntity entity)
        {
            _collection.Add(entity);
            return entity;
        }


        public TEntity Attach(TEntity entity)
        {
            _collection.Add(entity);
            return entity;
        }

        public TDerivedEntity Create<TDerivedEntity>() where TDerivedEntity : class, TEntity
        {
            return Activator.CreateInstance<TDerivedEntity>();
        }

        public TEntity Create()
        {
            return Activator.CreateInstance<TEntity>();
        }

        public TEntity Find(params object[] keyValues)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<TEntity> Local
        {
            get { return _collection; }
        }

        public TEntity Remove(TEntity entity)
        {
            _collection.Remove(entity);
            return entity;
        }

        // Get the dbSet enumerator 
        public IEnumerator<TEntity> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public Type ElementType
        {
            get { return _query.ElementType; }
        }

        public Expression Expression
        {
            get { return _query.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _query.Provider; }
        }
    }
}