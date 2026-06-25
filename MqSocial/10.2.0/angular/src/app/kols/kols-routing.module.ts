import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { KolsComponent } from './kols.component';

const routes: Routes = [
    { path: '', component: KolsComponent, pathMatch: 'full' },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class KolsRoutingModule {}
