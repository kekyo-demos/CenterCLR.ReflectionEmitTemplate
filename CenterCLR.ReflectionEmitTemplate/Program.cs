using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionEmitTemplate
{
	/// <summary>
	/// コードをILで動的に生成するヘルパークラスです。
	/// </summary>
	internal sealed class Emitter
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
			assemblyBuilder_ = AppDomain.CurrentDomain.DefineDynamicAssembly(
				assemblyName,
				AssemblyBuilderAccess.RunAndSave);
			moduleBuilder_ = assemblyBuilder_.DefineDynamicModule(name + ".dll");
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
			var type = typeBuilder.CreateType();

			// メソッドを取得する
			var bindingFlags = BindingFlags.Public | BindingFlags.Static;
			var method = type.GetMethod(methodName, bindingFlags);

			// デリゲートを生成する
			return (Func<TArgument, TReturn>) Delegate.CreateDelegate(
				typeof (Func<TArgument, TReturn>),
				method);
		}
	}

	public static class Program
	{
		public static void Main(string[] args)
		{
			// 動的アセンブリを生成
			var emitter = new Emitter("TestAssembly");

			// メソッドを動的に生成
			var func = emitter.EmitMethod<int, string>(
				"TestType",
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
