# AI agent skills for RepositoryExample

This repository ships two [Agent Skills](https://agentskills.io/) as portable Markdown instructions:

| Skill | Purpose | File |
| --- | --- | --- |
| `repositoryexample-query-objects` | Create/review native LINQ query objects with `IQuery`, `IQueryExecutor`, `IContextQuery`, or their context executors | [SKILL.md](../.agents/skills/repositoryexample-query-objects/SKILL.md) |
| `migrate-ardalis-to-repositoryexample` | Migrate Ardalis.Specification 9.x queries, repository calls, and save semantics to RepositoryExample | [SKILL.md](../.agents/skills/migrate-ardalis-to-repositoryexample/SKILL.md) |

These are **instructions for AI coding agents**, not C# runtime packages. They do not change the example's repository contracts or introduce an Ardalis dependency in production code.

## Use the skills

- **In this repository:** Codex discovers skills from `.agents/skills` at the repository root. Restart the agent if it does not pick up newly added skills.
- **In another project:** copy the desired skill folder into that project's `.agents/skills/` (Codex). For other agents, use the skill location supported by your tooling; for example, Copilot supports `.github/skills/` and Claude supports `.claude/skills/`. Keep the folder name matching the `name` in the YAML frontmatter.
- Ensure the target project contains or adapts the `IQuery`/`IRepository` contracts from this example. The skill alone does not add a library or install those interfaces.

You can ask the agent to apply a skill by name, or describe a matching task and let a skills-capable agent select it.

## Sample prompts

**Create a new query**

> Use `repositoryexample-query-objects` to add a reusable ProductsByCategory query with an EF-translatable filter, deterministic ordering, a small DTO projection, and SQLite integration tests. Use the existing IRepository contracts and pass a CancellationToken.

**Migrate an Ardalis specification**

> Use `migrate-ardalis-to-repositoryexample` to replace ProductsPageSpec and its ListAsync/CountAsync callers. Preserve all filters, count before Skip/Take, ordering, tracking and SaveChanges boundaries. Add side-by-side SQLite parity tests before removing the Ardalis dependency.

## Review / smoke checks

1. Confirm each `SKILL.md` has YAML frontmatter with a unique `name` equal to its parent folder and a trigger-oriented `description`.
2. Ask for an entity-list, DTO-list, scalar, and page/count query. The agent should choose `IQuery<TEntity>`, `IQuery<TEntity,TItem>`, and `IQueryExecutor<TEntity,TResult>` appropriately.
3. Ask to migrate a paged Ardalis spec. The agent must count the filtered **unpaged** source and explicitly address SaveChanges when migrating writes.
4. Verify that the agent uses native LINQ/EF Core and never invents an Ardalis-compatible Query builder or an additional specification evaluator.
5. Run [the integration tests](testing.md), and verify provider-specific SQL/collation on the actual database when important.

For complete API details, see [architecture](architecture.md), [the Ardalis migration guide](ardalis-migration.md), and [feature compatibility](feature-compatibility.md).
