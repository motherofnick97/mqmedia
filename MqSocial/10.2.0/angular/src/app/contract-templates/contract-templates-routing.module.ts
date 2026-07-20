import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ContractTemplatesComponent } from './contract-templates.component';

const routes: Routes = [
    { path: '', component: ContractTemplatesComponent, pathMatch: 'full' },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ContractTemplatesRoutingModule {}
