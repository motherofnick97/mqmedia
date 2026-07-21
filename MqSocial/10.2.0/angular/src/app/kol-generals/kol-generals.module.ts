import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { KolGeneralsRoutingModule } from './kol-generals-routing.module';
import { KolGeneralsComponent } from './kol-generals.component';
import { CreateKolGeneralDialogComponent } from './create-kol-general/create-kol-general-dialog.component';
import { EditKolGeneralDialogComponent } from './edit-kol-general/edit-kol-general-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        KolGeneralsRoutingModule,
        KolGeneralsComponent,
        CreateKolGeneralDialogComponent,
        EditKolGeneralDialogComponent,
    ],
})
export class KolGeneralsModule {}
