﻿// -----------------------------------------------------------------------
//  <copyright file="UnitOfWork.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using Raven.Tests.Core.Utils.Entities;

using Xunit;

namespace Raven.Tests.Core.Session
{
	public class UnitOfWork : RavenCoreTestBase
	{
		[Fact]
		public void Changes()
		{
			using (var store = GetDocumentStore())
			{
				using (var session = store.OpenSession())
				{
					Assert.False(session.Advanced.HasChanges);

					var user = new User { Id = "users/1", Name = "John" };
					session.Store(user);

					Assert.True(session.Advanced.HasChanged(user));
					Assert.True(session.Advanced.HasChanges);

					session.SaveChanges();

					Assert.False(session.Advanced.HasChanged(user));
					Assert.False(session.Advanced.HasChanges);

					user.AddressId = "addresses/1";
					Assert.True(session.Advanced.HasChanged(user));
					Assert.True(session.Advanced.HasChanges);

					session.Advanced.Clear();
					Assert.False(session.Advanced.HasChanges);
				}
			}
		}

		[Fact]
		public void Evict()
		{
			using (var store = GetDocumentStore())
			{
				using (var session = store.OpenSession())
				{
					var user = new User { Id = "users/1", Name = "John" };

					session.Store(user);
					session.SaveChanges();
				}

				using (var session = store.OpenSession())
				{
					Assert.Equal(0, session.Advanced.NumberOfRequests);

					session.Load<User>("users/1");

					Assert.Equal(1, session.Advanced.NumberOfRequests);

					var user = session.Load<User>("users/1");

					Assert.Equal(1, session.Advanced.NumberOfRequests);

					session.Advanced.Evict(user);

					session.Load<User>("users/1");

					Assert.Equal(2, session.Advanced.NumberOfRequests);
				}
			}
		}

		[Fact]
		public void Clear()
		{
			using (var store = GetDocumentStore())
			{
				using (var session = store.OpenSession())
				{
					var user = new User { Id = "users/1", Name = "John" };

					session.Store(user);
					session.SaveChanges();
				}

				using (var session = store.OpenSession())
				{
					Assert.Equal(0, session.Advanced.NumberOfRequests);

					session.Load<User>("users/1");

					Assert.Equal(1, session.Advanced.NumberOfRequests);

					session.Load<User>("users/1");

					Assert.Equal(1, session.Advanced.NumberOfRequests);

					session.Advanced.Clear();

					session.Load<User>("users/1");

					Assert.Equal(2, session.Advanced.NumberOfRequests);
				}
			}
		}

		[Fact]
		public void IsLoaded()
		{
			using (var store = GetDocumentStore())
			{
				using (var session = store.OpenSession())
				{
					var user = new User { Id = "users/1", Name = "John" };

					session.Store(user);
					session.SaveChanges();
				}

				using (var session = store.OpenSession())
				{
					Assert.False(session.Advanced.IsLoaded("users/1"));

					session.Load<User>("users/1");

					Assert.True(session.Advanced.IsLoaded("users/1"));
					Assert.False(session.Advanced.IsLoaded("users/2"));

					session.Advanced.Clear();

					Assert.False(session.Advanced.IsLoaded("users/1"));
				}
			}
		}

		[Fact]
		public void Refresh()
		{
			using (var store = GetDocumentStore())
			{
				using (var session = store.OpenSession())
				{
					var user = new User { Id = "users/1", Name = "John" };

					session.Store(user);
					session.SaveChanges();
				}

				using (var session = store.OpenSession())
				{
					var user = session.Load<User>("users/1");

					Assert.NotNull(user);
					Assert.Equal("John", user.Name);

					var u = store.DatabaseCommands.Get("users/1");
					u.DataAsJson["Name"] = "Jonathan";
					store.DatabaseCommands.Put("users/1", u.Etag, u.DataAsJson, u.Metadata);

					user = session.Load<User>("users/1");

					Assert.NotNull(user);
					Assert.Equal("John", user.Name);

					session.Advanced.Refresh(user);

					Assert.NotNull(user);
					Assert.Equal("Jonathan", user.Name);
				}
			}
		}
	}
}