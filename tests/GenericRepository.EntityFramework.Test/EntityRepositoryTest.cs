///////////////////////////////////////////////////////////////////////////
///
/// Unit Tests for CRUD operations. Mocking the data set to avoid going 
/// to real DB
/// 
/// Using xUnit and Moq libraries
///
/// xUnit
/// [Facts] are tests which are always true
/// [Theory] are tests which are only true for a particular set of data
/// 
///////////////////////////////////////////////////////////////////////////

// microsoft .NET
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity.Validation;
using System.Diagnostics;

// 3rd party Libraries
using Xunit;
using Moq;

// Application Libraries
using GenericRepository.EntityFramework.Test.Infrastrucure;

public static class Globals
{
    /// <summary>
    /// Developer sets to True to log test result to file
    /// Set to false to log in the VS console
    /// </summary>
    public static bool LOG_TO_FILE = false;
}

namespace GenericRepository.EntityFramework.Test
{
    public class EntityRepositoryTest : IDisposable
    {
        // Log file variables
        private TextWriter tmp = null;
        private StreamWriter sw = null;

        /// <summary>
        /// Constructor. Create a Unit test log file
        /// </summary>
        public EntityRepositoryTest()
        {
            if (Globals.LOG_TO_FILE == true)
            {
                //Save defaults
                tmp = Console.Out;
                // Create a log file
                FileStream fs = new FileStream("UnitTests.log", FileMode.OpenOrCreate);
                sw = new StreamWriter(fs);
                Console.SetOut(sw);
            }
        }

        /// <summary>
        /// Destructor / dispose
        /// </summary>
        public void Dispose()
        {
            // Restore logging defaults
            if (sw != null)
            {
                sw.Flush();
                sw.Close();
                Console.SetOut(tmp);
            }
            GC.SuppressFinalize(this);
        }

        #region UnitTest Read
        /// <summary>
        /// Set records and get expected amount of records
        /// </summary>
        [Fact]
        public void TestRepositoryGetAll()
        {
            // Change this value to test adding x number of people
            int recordsToCreate = 2;

            // Arrange
            // 1. Create Mock dbSet
            FakeDbSet<Person> FakeDbSet = new FakeDbSet<Person>();
            // 2. Add records to Mock dbSet
            IEnumerable<Person> people = AddPeopleToFakeDbSet(FakeDbSet, recordsToCreate);
            // 3. Create Mock db context
            Mock<IPeopleContext> MockdDbContextObject = new Mock<IPeopleContext>();
            // 4. Create Fake Repository from Fake dbSet
            EntityRepository<Person> repository = CreateRepository(MockdDbContextObject, FakeDbSet);

            // Act
            // Get all records from repository
            IEnumerable<Person> peopleResult = repository.GetAll().ToList();

            // Assert
            // Set must be called
            MockdDbContextObject.Verify(pc => pc.Set<Person>(), Times.Once());
            // Repository count must match Mock dbSet count
            Assert.Equal(FakeDbSet.Count(), peopleResult.Count());

            // Log results.
            var log = (Globals.LOG_TO_FILE == true ? sw : Console.Out);
            log.WriteLine("{0}(): Passed - Record stored: {1}, Records read: {2}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, FakeDbSet.Count(), peopleResult.Count());
        }

        /// <summary>
        /// Create n records and expect a specific record from the created range
        /// </summary>
        [Fact]
        public void TestRepositoryGetSingle_Exist()
        {
            // Change this value to test adding x number of people
            int recordsToCreate = 3;
            var IdToVerify = 2;

            // Arrange
            // 1. Create Mock dbSet
            FakeDbSet<Person> FakeDbSet = new FakeDbSet<Person>();
            // 2. Add records to Mock dbSet
            IEnumerable<Person> people = AddPeopleToFakeDbSet(FakeDbSet, recordsToCreate);
            // 3. Create Mock db context
            Mock<IPeopleContext> MockdDbContextObject = new Mock<IPeopleContext>();
            // 4. Create Fake Repository from Fake dbSet
            EntityRepository<Person> repository = CreateRepository(MockdDbContextObject, FakeDbSet);

            // Act: check record match
            var expectedPerson = people.FirstOrDefault(x => x.Id == IdToVerify);
            Person storedPerson = repository.GetSingle(IdToVerify);

            // Assert
            MockdDbContextObject.Verify(pc => pc.Set<Person>(), Times.Once());
            Assert.Same(expectedPerson, storedPerson);

            // Log results.
            var log = (Globals.LOG_TO_FILE == true ? sw : Console.Out);
            log.WriteLine("{0}(): Passed - Expected record Id {1} match stored record id {2}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, expectedPerson.Id, storedPerson.Id);
        }

        [Fact]
        // Create records once and get specific record out of range
        // Expect Null
        public void TestRepositoryGetSingle_NotExist()
        {
            // Change this value to test adding x number of people
            // Change this value to test adding x number of people
            int recordsToCreate = 2;
            var IdToVerify = 3;

            // Arrange
            // 1. Create Mock dbSet
            FakeDbSet<Person> FakeDbSet = new FakeDbSet<Person>();
            // 2. Add records to Mock dbSet
            IEnumerable<Person> people = AddPeopleToFakeDbSet(FakeDbSet, recordsToCreate);
            // 3. Create Mock db context
            Mock<IPeopleContext> MockdDbContextObject = new Mock<IPeopleContext>();
            // 4. Create Fake Repository from Fake dbSet
            EntityRepository<Person> repository = CreateRepository(MockdDbContextObject, FakeDbSet);

            // Act
            Person person = repository.GetSingle(IdToVerify);

            // Assert : query once
            MockdDbContextObject.Verify(pc => pc.Set<Person>(), Times.Once());
            Assert.Null(person);

            // Log results.
            var log = (Globals.LOG_TO_FILE == true ? sw : Console.Out);
            log.WriteLine("{0}(): Passed - records created {1} record id {2} not found\n", System.Reflection.MethodBase.GetCurrentMethod().Name, people.Count(), IdToVerify);
        }
        #endregion UnitTest Read

        #region UnitTest Create
        /// <summary>
        /// Add a valid record Unit Test
        /// The tests must succeed by matching the store records
        /// </summary>
        /// <Prequisite>GetAll() must be tested</Prequisite>
        [Fact]
        public void TestRepository_Add_Success()
        {
            // Change this value to test adding x number of people
            int recordsToCreate = 2;

            // Arrange
            // 1. Create Mock dbSet
            FakeDbSet<Person> FakeDbSet = new FakeDbSet<Person>();
            // 2. Add records to Mock dbSet
            IEnumerable<Person> people = GetFakePeople(recordsToCreate).ToList();
            // 3. Create Mock db context
            Mock<IPeopleContext> MockdDbContextObject = new Mock<IPeopleContext>();
            // 4. Create Fake Repository from Fake dbSet
            EntityRepository<Person> repository = CreateRepository(MockdDbContextObject, FakeDbSet);

            // Act
            IEnumerator<Person> en = people.GetEnumerator();
            while (en.MoveNext())
                repository.Add(en.Current);

            IEnumerable<Person> StoredRecordList = repository.GetAll().ToList();

            // Assert
            // Repository count must match Mock dbSet count
            Assert.Equal(FakeDbSet.Count(), StoredRecordList.Count());

            //en = people.GetEnumerator();
            //while (en.MoveNext())
            //{
            //    Person expected = en.Current;
            //    //TODO: NO! get the count back only!!! this is not Unit test!!
            //    Person storedPerson = repository.GetSingle(expected.Id);
            //    Assert.Same(expected, storedPerson);
            //}
        }

        /// <summary>
        /// Add an invalid record
        /// The tests must succeed by Catching a NullReferenceException exception
        /// </summary>
        [Fact]
        public void TestRepository_Add_Failure()
        {
            try
            {
                // Arrange: Create an Empty DbSet and add a person
                EntityRepository<Person> repository = CreateFakeDbSet(0);

                // Act
                repository.Add(null);
            }
            catch (Exception ex)
            {
                // Assert
                var exType = ex.GetType();
                Assert.Equal(exType, typeof(NullReferenceException));
            }
        }
        #endregion UnitTest Create

        [Fact]
        // Delete an existing record from a range of record
        public void TestRepositoryDelete_Exist()
        {
            // Arrange
            EntityRepository<Person> repository = CreateFakeDbSet(2);
            int idToDelete = 2;

            // Act
            //repository.Delete(idToDelete);

            // Asset
             
        }

        [Fact]
        // Delete a non existent record from a range of record
        public void TestRepositoryDelete_NotExist()
        {

        }

        [Fact]
        // Update an existing record
        public void TestRepositoryUpdate()
        {
        }

        #region Helpers
        /// <summary>
        /// Create a new Dataset and add a list records
        /// </summary>
        /// <param name="people">List containing all the records to add to the fake dataset</param>
        /// <returns>FakeDbSet<Person></returns>
        private FakeDbSet<Person> GetFakePeopleDbSet(IEnumerable<Person> people)
        {
            // Create empty test dBSet
            var FakePeopleDbSet = new FakeDbSet<Person>();

            // Add list of test records
            foreach (var person in people)
                FakePeopleDbSet.Add(person);

            return FakePeopleDbSet;
        }

        /// <summary>
        /// Create Repository from mock dbSet
        /// </summary>
        /// <param name="FakeDbSet">Fake dbSet object</param>
        /// <returns>Created repository</returns>
        private EntityRepository<Person> CreateRepository(Mock<IPeopleContext> MockdDbContextObject, FakeDbSet<Person> FakeDbSet)
        {
            Assert.NotNull(FakeDbSet);
            Assert.NotNull(MockdDbContextObject);

            // the FakeDbSet<Person> is returned each time the Set Person is executed
            MockdDbContextObject.Setup(pc => pc.Set<Person>()).Returns(FakeDbSet);

            IPeopleContext MockPeopleDbContext = MockdDbContextObject.Object;
            // Create repository using Generic Repository class from Mocked DbContext
            EntityRepository<Person> repository = new EntityRepository<Person>(MockPeopleDbContext);

            return repository;
        }

        /// <summary>
        /// Add records to fake dbSet
        /// </summary>
        /// <param name="FakeDbSet">DbSet to add records </param>
        /// <param name="nbOfRecordToAdd">number of records to add</param>
        /// <returns>List of added records</returns>
        private IEnumerable<Person> AddPeopleToFakeDbSet(FakeDbSet<Person> FakeDbSet, int nbOfRecordToAdd)
        {
            Assert.NotNull(FakeDbSet);

            // Create test records
            var peopleAdded = GetFakePeople(nbOfRecordToAdd).ToList();
            // Populate dbSet
            foreach (var person in peopleAdded)
                FakeDbSet.Add(person);

            return peopleAdded;
        }

        // Create and return a dbSet from Generic Repository using a mock dbContext
        //private EntityRepository<TEntity> CreatedDbSet<TEntity>(int nbOfAddedRecord) where TEntity : class
        //{
        //    FakeDbSet<TEntity> personDbSet = null;

        //    // Create an empty dbSet
        //    if (nbOfAddedRecord == 0)
        //        personDbSet = new FakeDbSet<TEntity>();
        //    else
        //    {
        //        // Create test records
        //        var people = GetDummyPeople(nbOfAddedRecord).ToList();
        //        // Create dbSet in memory and populate
        //        personDbSet = GetPersonDbSet(people) as FakeDbSet<TEntity>;
        //    }

        //    // Create Mock contexts
        //    var dbContextMock = new Mock<IPeopleContext>();
        //    // Attach entity type and get records from fake dbSet 
        //    dbContextMock.Setup(pc => pc.Set<TEntity>()).Returns(personDbSet);

        //    // Create dbSet from Generic Repository class
        //    EntityRepository<TEntity> repository = new EntityRepository<TEntity>(dbContextMock.Object);
        //}

        // Create test records
        // Note use yield return to convert iterator to state machine for each record

        /// <summary>
        /// Create n fake person
        /// note that yield is needed to ensure the iteration until end of loop
        /// </summary>
        /// <param name="count">number of fake people to create</param>
        /// <returns>IEnumerable<Person>Person created</returns>
        private IEnumerable<Person> GetFakePeople(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                Person p = new Person
                    {
                        Id = i,
                        Name = string.Concat("Name", i),
                        Surname = string.Concat("Surname", i),
                        Age = 5 * i,
                        CreatedOn = DateTime.Parse("2016-10-20 18:48")
                    };

                yield return p;
             }
        }
        #endregion Helpers

        /// <summary>
        /// Create and return a Mocked Person dbSet repository from Generic Repository
        /// </summary>
        /// <param name="nbOfAddedRecord">Number of fake Records to add</param>
        /// <returns>EntityRepository<Person></returns>
        private EntityRepository<Person> CreateFakeDbSet(int nbOfAddedRecord)
        {
            FakeDbSet<Person> FakePeopleDbSet = null;

            // Create an empty dbSet
            if (nbOfAddedRecord == 0)
                FakePeopleDbSet = new FakeDbSet<Person>();
            else
            {
                // Create test records
                var people = GetFakePeople(nbOfAddedRecord).ToList();
                // Create dbSet in memory and populate
                FakePeopleDbSet = GetFakePeopleDbSet(people);
            }

            // Create Mock object and dbSet mock context
            var MockPeopleContextObject = new Mock<IPeopleContext>();
            // the FakeDbSet<Person> peopleDbSet is returned each time the Set Person is executed
            MockPeopleContextObject.Setup(pc => pc.Set<Person>()).Returns(FakePeopleDbSet);

            IPeopleContext MockPeopleDbContext = MockPeopleContextObject.Object;
            // Create repository using Generic Repository class from Mocked DbContext
            EntityRepository<Person> repository = new EntityRepository<Person>(MockPeopleDbContext);

            return repository;
        }

    }
}