# Chadwick.NET

A C#/.NET 10 port of `cwbox`, the traditional boxscore generator from the
[Chadwick](http://chadwick-bureau.com/) baseball play-by-play toolkit. It parses
Retrosheet DiamondWare play-by-play (`.EVN`/`.EVA`), roster (`.ROS`), and team
(`TEAM####`) files - directly from a `.zip` archive or from an already-extracted
directory - and produces a traditional plaintext boxscore for a specified game.

## Attribution

This project is a derivative work, translating the parsing, game-state
simulation, and boxscore-accumulation logic of Chadwick's `cwbox` from C into
C#. Chadwick itself is:

> Chadwick is written and maintained by T. L. Turocy (ted.turocy at gmail.com)
> at Chadwick Baseball Bureau (<http://www.chadwick-bureau.com>)

The original C source is not included in this repository; this port was built
by reading and translating it as a reference implementation. See
[`LICENSE`](LICENSE) for the license this project is distributed under as a
result.

## License

GNU General Public License, version 2, or (at your option) any later version -
the same terms Chadwick itself is licensed under. See [`LICENSE`](LICENSE) for
the full text.

## Structure

- **`Chadwick.NET`** - class library: Retrosheet file parsing (tokenizer,
  roster/team files, event files, play-text parser), the domain model, game-state
  simulation, and boxscore accumulation. No console- or UI-specific code, so it's
  usable from any front end.
- **`Chadwick.NET.Cwbox`** - console application: CLI plus the plaintext boxscore
  renderer.
- **`Chadwick.NET.Tests`** - MSTest unit and integration tests.

## Building and running

```
dotnet build ChadwickNet.slnx
dotnet run --project Chadwick.NET.Cwbox -- <zip-file-or-directory> <year> <game-id>
```

For example, given a Retrosheet season archive `1968eve.zip`:

```
dotnet run --project Chadwick.NET.Cwbox -- 1968eve.zip 1968 BAL196804100
```

## Tests

```
dotnet test Chadwick.NET.Tests
```

Most tests run against small, self-contained synthetic fixtures. A handful of
integration tests additionally run against real Retrosheet season archives if
one is present at `../data/{year}eve.zip` relative to the test binary; those
are skipped (not failed) when no such archive is available.
