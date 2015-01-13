// ilasm /dll InvalidFuncDelegateName.il 

.assembly InvalidFuncDelegateName
{
}

.class public auto ansi sealed System.Func`13<+TResult>
	extends [mscorlib]System.MulticastDelegate
{
	.method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed 
	{
	}

	.method public hidebysig newslot virtual instance !TResult Invoke () runtime managed 
	{
	}

	.method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed 
	{
	}

	.method public hidebysig newslot virtual instance !TResult EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed 
	{
	}
}