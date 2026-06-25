import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AppRouteGuard } from '@shared/auth/auth-route-guard';
import { AppComponent } from './app.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: AppComponent,
                children: [
                    {
                        path: 'home',
                        loadChildren: () => import('./home/home.module').then((m) => m.HomeModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'about',
                        loadChildren: () => import('./about/about.module').then((m) => m.AboutModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'users',
                        loadChildren: () => import('./users/users.module').then((m) => m.UsersModule),
                        data: { permission: 'Pages.Users' },
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'roles',
                        loadChildren: () => import('./roles/roles.module').then((m) => m.RolesModule),
                        data: { permission: 'Pages.Roles' },
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'tenants',
                        loadChildren: () => import('./tenants/tenants.module').then((m) => m.TenantsModule),
                        data: { permission: 'Pages.Tenants' },
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'contract-kols',
                        loadChildren: () => import('./contract-kols/contract-kols.module').then((m) => m.ContractKolsModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'contracts',
                        loadChildren: () => import('./contracts/contracts.module').then((m) => m.ContractsModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'kols',
                        loadChildren: () => import('./kols/kols.module').then((m) => m.KolsModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'companies',
                        loadChildren: () => import('./companies/companies.module').then((m) => m.CompaniesModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'campaigns',
                        loadChildren: () => import('./campaigns/campaigns.module').then((m) => m.CampaignsModule),
                        canActivate: [AppRouteGuard],
                    },
                    {
                        path: 'update-password',
                        loadChildren: () => import('./users/users.module').then((m) => m.UsersModule),
                        canActivate: [AppRouteGuard],
                    },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class AppRoutingModule {}
