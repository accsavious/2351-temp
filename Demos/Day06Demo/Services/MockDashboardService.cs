namespace Day06Demo.Services;

using BYSResults;
// Demo B — Step 5 [CODE LIVE]: Refactor the implementation to Result<T>.
//
// TODO Step 1: Add `using BYSResults;` at the top of this file.
// TODO Step 2: Change the return type to Task<Result<DashboardData>>.
// TODO Step 3: Build a Result<DashboardData> and return it with .WithValue():
//              var result = new Result<DashboardData>();
//              return result.WithValue(new DashboardData( ... ));


public class MockDashboardService : IDashboardService
{
    public async Task<Result<DashboardData>> GetDashboardDataAsync()
    {
        // Simulate network delay
        await Task.Delay(300);
        var result = new Result<DashboardData>();

        return result.WithValue(new DashboardData(
            NotificationCount: 5,
            ProductCount: 128,
            UserName: "Alice Johnson"));
    }
}
