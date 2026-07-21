import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { KolGeneralsComponent } from './kol-generals.component';

const routes: Routes = [
    { path: '', component: KolGeneralsComponent, pathMatch: 'full' },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class KolGeneralsRoutingModule {}
