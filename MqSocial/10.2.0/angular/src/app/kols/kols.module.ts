import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { KolsRoutingModule } from './kols-routing.module';
import { KolsComponent } from './kols.component';
import { CreateKolDialogComponent } from './create-kol/create-kol-dialog.component';
import { EditKolDialogComponent } from './edit-kol/edit-kol-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        KolsRoutingModule,
        KolsComponent,
        CreateKolDialogComponent,
        EditKolDialogComponent,
    ],
})
export class KolsModule {}
