using System.Collections.Concurrent;
using DoubleSharp.Linq;

namespace Tests;

public class LinqTests {
	[Test]
	public void Enumerate() {
		foreach(var (i, x) in Enumerable.Range(0, 1000).Enumerate())
			Assert.That(x, Is.EqualTo(i));
	}

	[Test]
	public void ToDictionary() {
		var data = new[] {
			("Alice", 10), 
			("Bob", 5),
			("Charlie", 8),
		};
		var dict = data.ToDictionary();
		foreach(var (name, rank) in data)
			Assert.That(rank, Is.EqualTo(dict[name]));
	}

	[Test]
	public void ForEach() {
		var sum = 0;
		Enumerable.Range(0, 1000).ForEach(i => sum += i);
		Assert.That(sum, Is.EqualTo(499500));
		var coll = new ConcurrentBag<int>();
		Enumerable.Range(0, 1000).AsParallel().ForEach(i => coll.Add(i));
		Assert.That(coll.Sum(), Is.EqualTo(499500));
	}

	[Test]
	public void DictGroupBy() {
		var data = new (int Rank, string Name)[] {
			(10, "Alice"),
			(5, "Bob"),
			(5, "Charlie")
		};
		var dict = data.DictGroupBy(x => x.Rank);
		Assert.Multiple(() => {
			Assert.That(dict, Has.Count.EqualTo(2));
			var rank5 = dict[5].OrderBy(x => x.Name).ToList();
			var rank10 = dict[10].ToList();
			Assert.That(rank10, Has.Count.EqualTo(1));
			Assert.That(rank5, Has.Count.EqualTo(2));
			Assert.That(rank10[0].Name, Is.EqualTo("Alice"));
			Assert.That(rank5[0].Name, Is.EqualTo("Bob"));
			Assert.That(rank5[1].Name, Is.EqualTo("Charlie"));
		});

		var dict2 = data.DictGroupBy(x => x.Rank, x => x.Name);
		Assert.Multiple(() => {
			Assert.That(dict2, Has.Count.EqualTo(2));
			var rank5 = dict2[5].Order().ToList();
			var rank10 = dict2[10].ToList();
			Assert.That(rank10, Has.Count.EqualTo(1));
			Assert.That(rank5, Has.Count.EqualTo(2));
			Assert.That(rank10[0], Is.EqualTo("Alice"));
			Assert.That(rank5[0], Is.EqualTo("Bob"));
			Assert.That(rank5[1], Is.EqualTo("Charlie"));
		});
	}

	[Test]
	public void DictGroupByLists() {
		var data = new (int Rank, string Name)[] {
			(10, "Alice"),
			(5, "Bob"),
			(5, "Charlie")
		};
		var dict = data.DictGroupByLists(x => x.Rank);
		Assert.Multiple(() => {
			Assert.That(dict, Has.Count.EqualTo(2));
			var rank5 = dict[5].OrderBy(x => x.Name).ToList();
			Assert.That(dict[10], Has.Count.EqualTo(1));
			Assert.That(rank5, Has.Count.EqualTo(2));
			Assert.That(dict[10][0].Name, Is.EqualTo("Alice"));
			Assert.That(rank5[0].Name, Is.EqualTo("Bob"));
			Assert.That(rank5[1].Name, Is.EqualTo("Charlie"));
		});

		var dict2 = data.DictGroupByLists(x => x.Rank, x => x.Name);
		Assert.Multiple(() => {
			Assert.That(dict2, Has.Count.EqualTo(2));
			var rank5 = dict2[5].Order().ToList();
			Assert.That(dict2[10], Has.Count.EqualTo(1));
			Assert.That(rank5, Has.Count.EqualTo(2));
			Assert.That(dict2[10][0], Is.EqualTo("Alice"));
			Assert.That(rank5[0], Is.EqualTo("Bob"));
			Assert.That(rank5[1], Is.EqualTo("Charlie"));
		});
	}

	[Test]
	public void Flatten() {
		var data = new[] {
			new[] { "Hello", "World!" },
			new[] { "How", "are", "you?" }
		};
		var str = string.Join(" ", data.Flatten());
		Assert.That(str, Is.EqualTo("Hello World! How are you?"));
	}

	[Test]
	public void ArgMax() {
		Assert.Multiple(() => {
			Assert.That(new[] { 1, 0, 0 }.ArgMax(), Is.EqualTo(0));
			Assert.That(new[] { 0, 1, 0 }.ArgMax(), Is.EqualTo(1));
			Assert.That(new[] { 0, 1, 1 }.ArgMax(), Is.EqualTo(1));
			Assert.That(new[] { 0, 0, 1 }.ArgMax(), Is.EqualTo(2));
		});
	}

	[Test]
	public void ArgMaxBy() {
		Assert.Multiple(() => {
			Assert.That(new[] { (1, false), (0, false), (0, false) }.ArgMax(x => x.Item1), Is.EqualTo(0));
			Assert.That(new[] { (0, false), (1, false), (0, false) }.ArgMax(x => x.Item1), Is.EqualTo(1));
			Assert.That(new[] { (0, false), (1, false), (1, false) }.ArgMax(x => x.Item1), Is.EqualTo(1));
			Assert.That(new[] { (0, false), (0, false), (1, false) }.ArgMax(x => x.Item1), Is.EqualTo(2));
		});
	}

	[Test]
	public void Range() {
		Assert.Multiple(() => {
			Assert.That(5.Range().ToArray(), Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
			Assert.That((3, 6).Range().ToArray(), Is.EquivalentTo(new[] { 3, 4, 5 }));
			Assert.That((0, 16, 5).Range().ToArray(), Is.EquivalentTo(new[] { 0, 5, 10, 15 }));
		});
	}

	[Test]
	public void Times() {
		Assert.Multiple(() => {
			var count = 0;
			1000.Times(() => { count++; });
			Assert.That(count, Is.EqualTo(1000));
			var sum = 0;
			1000.Times(i => { sum += i; });
			Assert.That(sum, Is.EqualTo(499500));
			
			Assert.That(1000.Times(() => 1).Sum(), Is.EqualTo(1000));
			Assert.That(1000.Times(i => i).Sum(), Is.EqualTo(499500));
		});
	}
}