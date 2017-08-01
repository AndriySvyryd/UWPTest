using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Verification
{
    public class Tests
    {
        protected Func<string, Task> WriteLine { get; }

        public Tests(Func<string, Task> output)
        {
            WriteLine = output;
        }

        public async Task CompiledQuery()
        {
            using (var context = new VerificationApplicationContext())
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                var blog = context.CreateBlog();
                blog.Name = "Hello World!";

                context.CreatePost(blog).Title = "First";

                var blog2 = context.CreateBlog();
                blog2.Name = "0x000 is the new black";

                var post2 = context.CreatePost(blog2);
                post2.Title = "Black is back";

                await context.SaveChangesAsync();
            }

            using (var context = new VerificationApplicationContext())
            {
                var query = EF.CompileQuery((VerificationApplicationContext c, string t)
                    => c.Set<Post>().Include(p => p.Blog).Where(p => p.TenantId == t));
                Assert.Equal(2, context.Blogs.Count());
                Assert.Equal(0, context.Set<Blog>().Local.Count());
                var posts = query(context, "1").ToList();
                Assert.Equal(2, posts.Count());
                Assert.Equal(2, context.Set<Post>().Local.Count());
            }
        }

        public async Task AnonymousTypeProjection()
        {
            using (var context = new VerificationApplicationContext())
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                var blog = context.CreateBlog();
                blog.Name = "Hello World!";

                await context.SaveChangesAsync();
            }

            using (var context = new VerificationApplicationContext())
            {
                var result = (from b in context.Blogs.Select(b => new { b })
                              from l in context.Blogs.Select(l => new { l })
                              where b.b.Id == l.l.Id
                              select new { b, l }).First();

                Assert.Same(result.b.b, result.l.l);
            }
        }

        protected class VerificationApplicationContext : DbContext
        {
            public DbSet<Blog> Blogs { get; set; }
            public DbSet<Post> Posts { get; set; }
            public DbSet<Tenant> Tenants { get; set; }

            public VerificationApplicationContext()
                : base()
            {
            }

            public VerificationApplicationContext(DbContextOptions<VerificationApplicationContext> options)
                : base(options)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                base.OnConfiguring(optionsBuilder);

                optionsBuilder.UseSqlite("Data Source=Verification.db")
                    .EnableSensitiveDataLogging()
                    .ConfigureWarnings(w => w.Default(WarningBehavior.Throw).Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning));
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Blog>(bb => { });

                modelBuilder.Entity<Post>(pb =>
                {
                    pb.HasOne(p => p.Blog).WithMany(b => b.Posts).OnDelete(DeleteBehavior.ClientSetNull);
                });

                modelBuilder.Entity<Post>();
            }

            public Blog CreateBlog()
            {
                var currentTenant = Tenants.Find("1");
                if (currentTenant == null)
                {
                    Tenants.Add(new Tenant { Id = "1" });
                }
                var blog = new Blog { TenantId = "1" };
                Blogs.Add(blog);
                return blog;
            }

            public Post CreatePost(Blog blog)
            {
                var post = new Post { Blog = blog, TenantId = blog.TenantId };
                Posts.Add(post);
                return post;
            }
        }

        protected class Blog
        {
            public string TenantId { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public ICollection<Post> Posts { get; set; }
            public Tenant Tenant { get; set; }
        }

        protected class Post
        {
            public string TenantId { get; set; }
            public int BlogId { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
            public Blog Blog { get; set; }
            public Tenant Tenant { get; set; }
        }

        protected class Tenant
        {
            public string Id { get; set; }
        }
    }
}
