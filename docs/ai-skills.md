# AI agent skills for RepositoryExample

This repository ships two [Agent Skills](https://agentskills.io/) as portable Markdown instructions:

| Skill | Purpose | File |
| --- | --- | --- |
| `repositoryexample-query-objects` | Create/review native LINQ query objects with `IQuery`, `IQueryExecutor`, `IContextQuery`, or their context executors | [SKILL.md](../.agents/skills/repositoryexample-query-objects/SKILL.md) |
| `migrate-ardalis-specification-to-repositoryexample` | Migrate Ardalis.Specification 9.x queries, repository calls, and save semantics to RepositoryExample | [SKILL.md](../.agents/skills/migrate-ardalis-specification-to-repositoryexample/SKILL.md) |

These are **instructions for AI coding agents**, not C# runtime packages. They do not change the example's repository contracts or introduce an Ardalis dependency in production code.

## Install the skills

Run the [Skills CLI](https://github.com/vercel-labs/skills) from your application repository (requires Node.js/npm):

```shell
# Discover and install either or both skills
npx skills add ycherkes/RepositoryExample

# Or install only the query-authoring skill
npx skills add ycherkes/RepositoryExample --skill repositoryexample-query-objects

# Or install only the Ardalis migration skill
npx skills add ycherkes/RepositoryExample --skill migrate-ardalis-specification-to-repositoryexample
```

The CLI lets you choose supported agents and installation scope. For a reproducible project setup, select project scope. The skills are stored in [`.agents/skills/`](../.agents/skills/) and are compatible with the [Agent Skills specification](https://agentskills.io/specification).

**In this repository:** supported agents, including Codex, can discover the skills directly from `.agents/skills/` without running the install command. Restart the coding agent if necessary after adding skills.

**Manual option:** copy either skill folder to your coding agent's supported skills directory, retaining its `SKILL.md` and metadata.

**Important:** these commands install *AI instructions*, not a NuGet library or the C# contracts. Your consumer project must already contain (or adapt) `IQuery`/`IRepository` from this example.

Ask the coding agent to apply a skill by name, or describe a matching task and let a skills-capable agent select it.

## Sample prompts

**Create a new query**

> Use `repositoryexample-query-objects` to add a reusable ProductsByCategory query with an EF-translatable filter, deterministic ordering, a small DTO projection, and SQLite integration tests. Use the existing IRepository contracts and pass a CancellationToken.

**Migrate an Ardalis specification**

> Use `migrate-ardalis-specification-to-repositoryexample` to replace ProductsPageSpec and its ListAsync/CountAsync callers. Preserve all filters, count before Skip/Take, ordering, tracking and SaveChanges boundaries. Add side-by-side SQLite parity tests before removing the Ardalis dependency.

## Review / smoke checks

1. Confirm each `SKILL.md` has YAML frontmatter with a unique `name` equal to its parent folder and a trigger-oriented `description`.
2. Ask for an entity-list, DTO-list, scalar, and page/count query. The agent should choose `IQuery<TEntity>`, `IQuery<TEntity,TItem>`, and `IQueryExecutor<TEntity,TResult>` appropriately.
3. Ask to migrate a paged Ardalis spec. The agent must count the filtered **unpaged** source and explicitly address SaveChanges when migrating writes.
4. Verify that the agent uses native LINQ/EF Core and never invents an Ardalis-compatible Query builder or an additional specification evaluator.
5. Run [the integration tests](testing.md), and verify provider-specific SQL/collation on the actual database when important.

For complete API details, see [architecture](architecture.md), [the Ardalis migration guide](ardalis-migration.md), and [feature compatibility](feature-compatibility.md).
