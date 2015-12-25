using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CenterCLR.ReflectionEmitTemplate
{
	/// <summary>
	/// コードをILで動的に生成するヘルパークラスです。
	/// </summary>
	internal sealed class Emitter : IDisposable
	{
		private readonly AssemblyBuilder assemblyBuilder_;
		private readonly ModuleBuilder moduleBuilder_;

		/// <summary>
		/// コンストラクタです。
		/// </summary>
		/// <param name="name">アセンブリ名</param>
		public Emitter(string name)
		{
			var assemblyName = new AssemblyName(name);
#if NET45
			assemblyBuilder_ = AssemblyBuilder.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.RunAndSave);
#else
			assemblyBuilder_ = AssemblyBuilder.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.Run);
#endif
			moduleBuilder_ = assemblyBuilder_.DefineDynamicModule(name + ".dll");
		}

		/// <summary>
		/// Disposeメソッドです。
		/// </summary>
		public void Dispose()
		{
#if NET45
			// デバッグ用に出力
			assemblyBuilder_.Save(moduleBuilder_.ScopeName);
#endif
		}

		/// <summary>
		/// 引数と戻り値を指定可能なメソッドを定義します。
		/// </summary>
		/// <typeparam name="TArgument">引数の型</typeparam>
		/// <typeparam name="TReturn">戻り値の型</typeparam>
		/// <param name="typeName">クラス名</param>
		/// <param name="methodName">メソッド名</param>
		/// <param name="emitter">Emitを実行するデリゲート</param>
		/// <returns>デリゲート</returns>
		public Func<TArgument, TReturn> EmitMethod<TArgument, TReturn>(
			string typeName,
			string methodName,
			Action<ILGenerator> emitter)
		{
			// クラス定義
			var typeAttribute = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract;
			var typeBuilder = moduleBuilder_.DefineType(
				typeName,
				typeAttribute,
				typeof(object));

			// メソッド定義
			var methodAttribute = MethodAttributes.Public | MethodAttributes.Static;
			var methodBuilder = typeBuilder.DefineMethod(
				methodName,
				methodAttribute,
				typeof(TReturn),
				new [] { typeof(TArgument) });

			// ILGenerator
			var ilGenerator = methodBuilder.GetILGenerator();

			emitter(ilGenerator);

			// 定義を完了して型を得る
			var typeInfo = typeBuilder.CreateTypeInfo();
			var type = typeInfo.AsType();

			// メソッドを取得する
			var bindingFlags = BindingFlags.Public | BindingFlags.Static;
			var method = type.GetMethod(methodName, bindingFlags);

			// デリゲートを生成する
			return (Func<TArgument, TReturn>)method.CreateDelegate(
				typeof(Func<TArgument, TReturn>));
		}
	}

	public static class Program
	{
		public static void Main(string[] args)
		{
			// 動的アセンブリを生成
			using (var emitter = new Emitter("TestAssembly"))
			{
				// メソッドを動的に生成
				var func = emitter.EmitMethod<int, string>(
					"TestNamespace.TestType",
					"TestMethod",
					ilGenerator =>
					{
						ilGenerator.Emit(OpCodes.Ldstr, "Hello IL coder!");
						ilGenerator.Emit(OpCodes.Ret);
					});

				// 実行
				var result = func(123);

				// 結果
				Console.WriteLine(result);
			}
		}
	}
}
