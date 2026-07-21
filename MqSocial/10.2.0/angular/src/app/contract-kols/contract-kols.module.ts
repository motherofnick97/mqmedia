import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { ContractKolsRoutingModule } from './contract-kols-routing.module';
import { ContractKolsComponent } from './contract-kols.component';
import { CreateContractKolDialogComponent } from './create-contract-kol/create-contract-kol-dialog.component';
import { EditContractKolDialogComponent } from './edit-contract-kol/edit-contract-kol-dialog.component';
import { ViewContractKolDetailDialogComponent } from './view-contract-kol-detail/view-contract-kol-detail-dialog.component';
import { ExportContractDialogComponent } from './export-contract/export-contract-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        ContractKolsRoutingModule,
        ContractKolsComponent,
        CreateContractKolDialogComponent,
        EditContractKolDialogComponent,
        ViewContractKolDetailDialogComponent,
        ExportContractDialogComponent,
    ],
})
export class ContractKolsModule {}
