import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ContractKolsComponent } from './contract-kols.component';

const routes: Routes = [
    { path: '', component: ContractKolsComponent, pathMatch: 'full' },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ContractKolsRoutingModule {}
