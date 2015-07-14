#line 1 "C:\Async.cs"
#pragma checksum "C:\Async.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

using System.Threading.Tasks;

public class C
{
	public async Task<int> M1() 
	{
		await Task.FromResult(0); 
		await Task.FromResult(1); 
		await Task.FromResult(2); 
		
		return 1;
    }
    
    public async void M2() 
	{
		await Task.FromResult(0); 
    }
}